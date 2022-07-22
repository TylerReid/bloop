﻿using Bloop.Core;
using System.Text.Json;
using System.Text.Json.Nodes;
using CommandLine;
using Error = Bloop.Core.Error;

namespace Bloop.Cli;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        var parseObject = CommandLine.Parser.Default.ParseArguments<RequestOptions, ListOptions>(args);
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
            return await Run(list);
        }

        Console.WriteLine("something bad happened, and we got these parsed types for cli options:");
        Console.WriteLine(parsedArgs.GetType());
        Console.WriteLine(parsedArgs.Value.GetType());

        return 1;
    }

    private static async Task<int> Run(ListOptions options)
    {
        var config = await ConfigLoader.LoadConfigAsync(options.ConfigPath);

        if ("all".StartsWith(options.Type) || "requests".StartsWith(options.Type))
        {
            Console.WriteLine("requests:");
            foreach (var (name, request) in config.Request)
            {
                Console.WriteLine($"{name}:\t{request}");
            }
            Console.WriteLine();
        }
        

        if ("all".StartsWith(options.Type) || "variables".StartsWith(options.Type))
        {
            Console.WriteLine("variables:");
            foreach (var (name, variable) in config.Variable)
            {
                Console.WriteLine($"{name}:\t{variable}");
            }
            Console.WriteLine();
        }

        return 0;
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