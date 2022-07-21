using Bloop.Core;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.CommandLine;

namespace Bloop.Cli;

public class Program
{
    public static async Task Main(string request, bool prettyPrint = true, bool verbose = false, string configPath = "bloop.toml")
    {
        var options = new RequestOptions
        {
            RequestName = request,
            PrettyPrint = prettyPrint,
            Verbose = verbose,
            ConfigPath = configPath,
        };

        var config = await ConfigLoader.LoadConfigAsync(options.ConfigPath);
        var blooper = new Blooper();

        var response = await blooper.SendRequest(config, options.RequestName);
        //todo MatchAsync
        if (response.Unwrap() is HttpResponseMessage r)
        {
            await PrintResponse(r, options);
        }
        else if (response.Unwrap() is Error e)
        {
            Console.WriteLine(e.Message);
        }
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