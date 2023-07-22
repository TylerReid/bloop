using System.Text.RegularExpressions;
using System.Diagnostics;

namespace Bloop.Core;

public class VariableHandler
{
    private static readonly Regex VarRegex = new Regex(@"\${(?<var>\w+)}", RegexOptions.Compiled);

    public static List<string> GetVariables(string s) => VarRegex.Matches(s)
        .Select(x => x.Groups["var"])
        .Select(x => x.Value)
        .Distinct()
        .ToList();

    public static List<string> GetVariables(Defaults defaults, Request request) => request.Headers.Values.SelectMany(GetVariables)
        .Concat(defaults.Headers.Values.SelectMany(GetVariables))
        .Concat(GetVariables(request.Body ?? ""))
        .Concat(GetVariables(request.Uri))
        .Concat(request.Form?.Values.SelectMany(GetVariables) ?? Enumerable.Empty<string>())
        .Distinct()
        .ToList();

    public static string ExpandVariables(string original, Config config) => 
        ExpandVariables(original, config.Variables.ToDictionary(x => x.Name, x => x.Value!));

    public static string ExpandVariables(string original, Dictionary<string, string> mappings)
    {
        foreach (var (name, value) in mappings)
        {
            original = original.Replace($"${{{name}}}", value);
        }
        return original;
    }

    public static List<Variable> UnsatisfiedVariables(Config config) => config.Variables
        .Where(x => x.Value == null)
        .ToList();

    public static List<Variable> SatisfiedVariables(Config config) => config.Variables
        .Where(x => x.Value is string)
        .ToList();

    public static async Task<Either<Unit, Error>> RunCommand(Variable variable)
    {
        if (variable.Command == null)
        {
            return new Error("variable command was unexpectedly null");
        }

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = variable.Command,
                Arguments = variable.CommandArgs,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            },
        };

        try
        {
            process.Start();
            await process.WaitForExitAsync();
            if (process.ExitCode != 0)
            {
                return new Error($"variable command exited with code {process.ExitCode} and output:\n{await process.StandardError.ReadToEndAsync()}");
            }

            variable.Value = await process.StandardOutput.ReadToEndAsync();

            return Unit.Instance;
        }
        catch (Exception e)
        {
            return new Error($"error running variable command: {e.Message}");
        }
    }

    public static async Task<Either<Unit, Error>> SatisfyVariables(Blooper blooper, Config config, Request request)
    {
        var variables = GetVariables(config.Defaults, request);
        foreach (var v in variables)
        {
            var variable = config.Variables.SingleOrDefault(x => x.Name == v);

            if (variable == null)
            {
                return new Error($"variable {v} is used in request {request.Uri} but is not defined as a variable");
            }

            if (variable.Value != null)
            {
                continue;
            }

            if (variable.Jpath != null && variable.Source != null)
            {
                // if the source is this request, don't bloop because that will cause infinite loop
                // one legitimate way to end up in this scenario is with default headers
                if (request.Name == variable.Source)
                {
                    continue;
                }
                var response = await blooper.SendRequest(config, variable.Source);
                if (response.Unwrap() is Error e)
                {
                    return e;
                }
                continue;
            }

            if (variable.Command != null)
            {
                var commandResult = await RunCommand(variable);
                if (commandResult.Unwrap() is Error e)
                {
                    return e;
                }
                continue;
            }

            if (variable.Env != null)
            {
                variable.Value = Environment.GetEnvironmentVariable(variable.Env);
                if (variable.Value != null)
                {
                    continue;
                }
            }

            if (variable.File != null)
            {
                try
                {
                    variable.Value = await File.ReadAllTextAsync(variable.File);
                    continue;
                }
                catch (Exception e) when (variable.Default == null)
                {
                    return new Error($"error loading variable from file {variable.File}: {e.Message}");
                }
                catch (FileNotFoundException)
                {
                    //ignore because we have a default
                }
            }

            if (variable.Default != null)
            {
                variable.Value = variable.Default;
                continue;
            }

            return new Error($"variable {v} does not have a value, file, jpath, or command defined");
        }

        return Unit.Instance;
    }
}