﻿using Bloop.Core;
using System.Text.Json;
using System.Text.Json.Nodes;
using CommandLine;
using Error = Bloop.Core.Error;
using Terminal.Gui;
using Bloop.Cli.Ui;

namespace Bloop.Cli;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        var parseObject = Parser.Default.ParseArguments<RequestOptions, ListOptions, ValidateOptions, VariableOptions>(args);
        var parsedArgs = parseObject as Parsed<object>;
        if (parsedArgs == null)
        {
            return 1;
        }

        if (parsedArgs.Value is BaseOptions baseOptions)
        {
            if (baseOptions.NoColor)
            {
                Output.WriteColors = false;
            }
        }

        if (parsedArgs.Value is RequestOptions request)
        {
            if (string.IsNullOrEmpty(request.RequestName))
            {
                return RunUi(request);
            }
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

        if (parsedArgs.Value is VariableOptions variable)
        {
            return await Variable(variable);
        }

        Output.WriteError("something bad happened, and we got these parsed types for cli options:");
        Output.WriteLine(parsedArgs.GetType());
        Output.WriteLine(parsedArgs.Value.GetType());

        return 1;
    }

    private static int RunUi(RequestOptions request)
    {
        // pass the request options to the ui window
        Application.Run<MainWindow>();
        Application.Shutdown();
        return 0;
    }

    private static async Task<int> Validate(ValidateOptions options)
    {
        var configLoad = await ConfigLoader.LoadConfigAsync(options.ConfigPath ?? Environment.CurrentDirectory);
        if (configLoad.Unwrap() is Error error)
        {
            Output.WriteError(error.Message);
            return 1;
        }

        var config = configLoad.UnwrapSuccess();
        var failures = Validator.Validate(config);
        foreach (var failure in failures)
        {
            Output.WriteError(failure.Message);
        }
        return failures.Any() ? 1 : 0;
    }

    private static async Task<int> List(ListOptions options)
    {
        var configLoad = await ConfigLoader.LoadConfigAsync(options.ConfigPath ?? Environment.CurrentDirectory);

        return configLoad.Match(
            config => {
                if ("all".StartsWith(options.Type) || "requests".StartsWith(options.Type))
                {
                    foreach (var request in config.Requests)
                    {
                        Output.Write("[");
                        Output.Write("request", ConsoleColor.Green);
                        Output.Write(".");
                        Output.Write($"{request.Name}", ConsoleColor.White);
                        Output.WriteLine("]");
                        Output.WriteLine($"{request}");
                    }
                    Output.WriteLine();
                }
                

                if ("all".StartsWith(options.Type) || "variables".StartsWith(options.Type))
                {
                    foreach (var variable in config.Variables)
                    {
                        Output.Write("[");
                        Output.Write("variable", ConsoleColor.Green);
                        Output.Write(".");
                        Output.Write($"{variable.Name}", ConsoleColor.White);
                        Output.WriteLine("]");
                        Output.WriteLine($"{variable}");
                    }
                    Output.WriteLine();
                }

                if ("all".StartsWith(options.Type) || "defaults".StartsWith(options.Type))
                {
                    if (config.Defaults.Headers.Any())
                    {
                        Output.WriteLine("default headers", ConsoleColor.Green);
                        foreach (var (name, value) in config.Defaults.Headers)
                        {
                            Output.Write($"{name}: ", ConsoleColor.White);
                            Output.WriteLine($"{value}");
                        }
                        Output.WriteLine();
                    }
                }

                return 0;
            },
            error => {
                Output.WriteError(error.Message);
                return 1;
            }
        );
    }

    private static async Task<int> Run(RequestOptions options)
    {
        var configLoad = await ConfigLoader.LoadConfigAsync(options.ConfigPath ?? Environment.CurrentDirectory);

        if (configLoad.Unwrap() is Error error)
        {
            Output.WriteError(error.Message);
            return 1;
        }

        var config = configLoad.UnwrapSuccess();
        config.Env = options.VariableSet;

        foreach (var optionVar in options.Variables ?? Enumerable.Empty<string>())
        {
            var split = optionVar.Split('=');
            if (split?.Count() != 2)
            {
                Output.WriteError($"invalid variable: {optionVar}\nexpected variables to be in the form of someKey=SomeValue");
                return 1;
            }
            var variable = config.Variables.SingleOrDefault(x => x.Name == split[0]);
            if (variable != null)
            {
                variable.Value = split[1];
            }
        }

        var blooper = CreateBlooper(options);

        var response = await blooper.SendRequest(config, options.RequestName);

        return await response.MatchAsync<int>(
            async r => {
                await PrintResponse(r, options);
                return 0;
            },
            e => {
                Output.WriteError(e.Message);
                return Task.FromResult(1);
            }
        );
    }

    private static async Task<int> Variable(VariableOptions options)
    {
        var configLoad = await ConfigLoader.LoadConfigAsync(options.ConfigPath ?? Environment.CurrentDirectory);

        if (configLoad.Unwrap() is Error error)
        {
            Output.WriteError(error.Message);
            return 1;
        }

        var config = configLoad.UnwrapSuccess();
        config.Env = options.VariableSet;
        var blooper = CreateBlooper(options);
        
        var variable = config.Variables.FirstOrDefault(x => x.Name == options.VariableName);
        if (variable is null)
        {
            Output.WriteError($"No variable named {options.VariableName} found");
            return 1;
        }
        
        var result = await VariableHandler.SatisfyVariable(blooper, config, "cli", variable);

        if (result is not null)
        {
            Output.WriteError(result.Message);
            return 1;
        }
        
        Output.WriteLine(variable.Value!);
        
        return 0;
    }

    private static Blooper CreateBlooper(BaseOptions options)
    {
        var insecureHandler = new HttpClientHandler();
        insecureHandler.ServerCertificateCustomValidationCallback = (a, b, c, d) => true;
        var client = options.Insecure
            ? new HttpClient(insecureHandler)
            : new HttpClient();
        var blooper = new Blooper(client);
        return blooper;
    }

    static async Task PrintResponse(HttpResponseMessage response, RequestOptions options)
    {
        if (options.Verbose)
        {
            Output.WriteLine($"Request Uri: {response.RequestMessage?.RequestUri}", ConsoleColor.White);
            Output.WriteLine($"Status: {response.StatusCode}", ConsoleColor.White);
            Output.WriteLine();
        }

        var isJson = response.Content?.Headers?.ContentType?.MediaType == "application/json";
        if (isJson && options.PrettyPrint)
        {
            var parsedJson = JsonNode.Parse(await response.Content!.ReadAsStreamAsync());
            var json = parsedJson!.ToJsonString(options: new JsonSerializerOptions
            {
                WriteIndented = true,
            });
            Output.WriteLine(json);
        }
        else
        {
            var content = await response.Content!.ReadAsStringAsync();
            Output.WriteLine(content);
        }
    }
}