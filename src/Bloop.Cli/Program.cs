using Bloop.Core;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Bloop.Cli;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        var parsedArgs = CommandLine.Parser.Default.ParseArguments<RequestOptions>(args);

        return parsedArgs.Tag switch
        {
            CommandLine.ParserResultType.Parsed => await Run(parsedArgs.Value),
            _ => 1,
        };
    }

    private static async Task<int> Run(RequestOptions options)
    {
        var config = await ConfigLoader.LoadConfigAsync(options.ConfigPath);

        var insecureHandler = new HttpClientHandler();
        insecureHandler.ServerCertificateCustomValidationCallback = (a, b, c, d) => true;
        var client = options.Insecure
            ? new HttpClient(insecureHandler)
            : new HttpClient();
        var blooper = new Blooper(client);

        var response = await blooper.SendRequest(config, options.RequestName);
        //todo MatchAsync
        if (response.Unwrap() is HttpResponseMessage r)
        {
            await PrintResponse(r, options);
        }
        else if (response.Unwrap() is Error e)
        {
            Console.WriteLine(e.Message);
            return 1;
        }

        return 0;
    }

    static async Task PrintResponse(HttpResponseMessage response, RequestOptions options)
    {
        if (options.Verbose)
        {
            Console.WriteLine($"Status: {response.StatusCode}");
            Console.WriteLine();
        }

        var isJson = response.Content?.Headers?.ContentType?.MediaType == "application/json";
        if (isJson && options.PrettyPrint)
        {
            var parsedJson = JsonNode.Parse(await response.Content!.ReadAsStreamAsync());
            var json = parsedJson!.ToJsonString(options: new JsonSerializerOptions
            {
                WriteIndented = true,
            });
            Console.WriteLine(json);
        }
        else
        {
            var content = await response.Content!.ReadAsStringAsync();
            Console.WriteLine(content);
        }
    }
}