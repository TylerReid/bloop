using Bloop.Core;
using System.Text.Json;
using System.Text.Json.Nodes;
using CommandLine;
using Error = Bloop.Core.Error;

namespace Bloop.Cli;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        var parseObject = CommandLine.Parser.Default.ParseArguments<RequestOptions, ListOptions, ValidateOptions>(args);
        var parsedArgs = parseObject as Parsed<object>;
        if (parsedArgs == null)
        {
            return 1;
        }

        if (parsedArgs.Value is RequestOptions request)
        {
            return await Run(request);
        }

        if (parsedArgs.Value is ListOptions list)
        {
            return await List(list);
        }

        if (parsedArgs.Value is ValidateOptions validate)
        {
            return await Validate(validate);
        }

        Console.WriteLine("something bad happened, and we got these parsed types for cli options:");
        Console.WriteLine(parsedArgs.GetType());
        Console.WriteLine(parsedArgs.Value.GetType());

        return 1;
    }

    private static async Task<int> Validate(ValidateOptions options)
    {
        var configLoad = await ConfigLoader.LoadConfigAsync(options.ConfigPath);
        if (configLoad.Unwrap() is Error error)
        {
            Console.WriteLine(error.Message);
            return 1;
        }

        var config = configLoad.UnwrapSuccess();
        var failures = Validator.Validate(config);
        foreach (var failure in failures)
        {
            Console.WriteLine(failure.Message);
        }
        return failures.Any() ? 1 : 0;
    }

    private static async Task<int> List(ListOptions options)
    {
        var configLoad = await ConfigLoader.LoadConfigAsync(options.ConfigPath);

        return configLoad.Match(
            config => {
                if ("all".StartsWith(options.Type) || "requests".StartsWith(options.Type))
                {
                    Console.WriteLine("requests:");
                    foreach (var (name, request) in config!.Request)
                    {
                        Console.WriteLine($"{name}:\t{request}");
                    }
                    Console.WriteLine();
                }
                

                if ("all".StartsWith(options.Type) || "variables".StartsWith(options.Type))
                {
                    Console.WriteLine("variables:");
                    foreach (var (name, variable) in config!.Variable)
                    {
                        Console.WriteLine($"{name}:\t{variable}");
                    }
                    Console.WriteLine();
                }

                return 0;
            },
            error => {
                Console.WriteLine(error.Message);
                return 1;
            }
        );
    }

    private static async Task<int> Run(RequestOptions options)
    {
        var configLoad = await ConfigLoader.LoadConfigAsync(options.ConfigPath);

        if (configLoad.Unwrap() is Error error)
        {
            Console.WriteLine(error.Message);
            return 1;
        }

        var config = configLoad.UnwrapSuccess();

        foreach (var optionVar in options.Variables ?? Enumerable.Empty<string>())
        {
            var split = optionVar.Split('=');
            if (split?.Count() != 2)
            {
                Console.WriteLine($"invalid variable: {optionVar}\nexpected variables to be in the form of someKey=SomeValue");
                return 1;
            }
            if (config.Variable.ContainsKey(split[0]))
            {
                config.Variable[split[0]].Value = split[1];
            }
        }

        var insecureHandler = new HttpClientHandler();
        insecureHandler.ServerCertificateCustomValidationCallback = (a, b, c, d) => true;
        var client = options.Insecure
            ? new HttpClient(insecureHandler)
            : new HttpClient();
        var blooper = new Blooper(client);

        var response = await blooper.SendRequest(config, options.RequestName);

        return await response.MatchAsync<int>(
            async r => {
                await PrintResponse(r, options);
                return 0;
            },
            e => {
                Console.WriteLine(e.Message);
                return Task.FromResult(1);
            }
        );
    }

    static async Task PrintResponse(HttpResponseMessage response, RequestOptions options)
    {
        if (options.Verbose)
        {
            Console.WriteLine($"Request Uri: {response.RequestMessage?.RequestUri}");
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