using System.Text.RegularExpressions;
using System.Diagnostics;

namespace Bloop.Core;

public partial class VariableHandler
{
    private static readonly Regex VarRegex = VariableRegex();

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

    public static string? ExpandVariables(string? original, Config config, Func<string, string>? transformer = null) =>
        ExpandVariables(original, config.Variables.ToDictionary(x => x.Name, x => x.Value!), transformer);

    public static string? ExpandVariables(string? original, Dictionary<string, string> mappings, Func<string, string>? transformer = null)
    {
        if (transformer == null) { transformer = s => s; }
        foreach (var (name, value) in mappings)
        {
            original = original?.Replace($"${{{name}}}", transformer(value));
        }
        return original;
    }

    public static List<Variable> UnsatisfiedVariables(Config config) => config.Variables
        .Where(x => x.Value == null)
        .ToList();

    public static List<Variable> SatisfiedVariables(Config config) => config.Variables
        .Where(x => x.Value is string)
        .ToList();

    public static async Task<Either<Unit, Error>> SatisfyVariables(Blooper blooper, Config config, Request request)
    {
        // todo make this not dumb.
        // probably need to create a dependency graph of the variables and requests and verify there are no cycles
        // then we can satisfy them in a reasonable order
        var variables = GetVariables(config.Defaults, request);
        foreach (var v in variables)
        {
            var variable = config.Variables.SingleOrDefault(x => x.Name == v);
            if (variable == null)
            {
                return new Error($"variable {v} is used in request {request.Uri} but is not defined as a variable");
            }
            var result = await SatisfyVariable(blooper, config, request.Name, variable);
            if (result != null)
            {
                return result;
            }
        }

        return Unit.Instance;
    }

    private static async Task<Error?> SatisfyVariable(Blooper blooper, Config config, string sourceName, Variable variable)
    {
        variable.ClearIfExpired();

        if (variable.Value != null)
        {
            return null;
        }

        if (variable.Jpath != null && variable.Source != null)
        {
            // if the source is this request, don't bloop because that will cause infinite loop
            // one legitimate way to end up in this scenario is with default headers
            if (sourceName == variable.Source)
            {
                return null;
            }
            var response = await blooper.SendRequest(config, variable.Source);
            if (response.Unwrap() is Error e)
            {
                return e;
            }
            return null;
        }

        if (variable.Command != null)
        {
            var commandResult = await RunCommand(variable, config);
            if (commandResult.Unwrap() is Error e)
            {
                return e;
            }
            return null;
        }

        if (variable.Env != null)
        {
            variable.Value = Environment.GetEnvironmentVariable(variable.Env);
            if (variable.Value != null)
            {
                return null;
            }
        }

        if (variable.File != null)
        {
            try
            {
                var path = Path.GetFullPath(variable.File, config.Directory);
                variable.Value = await File.ReadAllTextAsync(path);
                return null;
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
            var variableVariables = GetVariables(variable.Default);
            foreach (var variableVariable in UnsatisfiedVariables(config).Where(x => variableVariables.Contains(x.Name)))
            {
                var result = await SatisfyVariable(blooper, config, variable.Name, variableVariable);
                if (result != null)
                {
                    return result;
                }
            }

            variable.Value = ExpandVariables(variable.Default, config);
            
            return null;
        }

        return new Error($"variable {variable.Name} does not have a value, file, jpath, or command defined");
    }

    private static async Task<Either<Unit, Error>> RunCommand(Variable variable, Config config)
    {
        if (variable.Command == null)
        {
            return new Error("variable command was unexpectedly null");
        }

        var commandPath = Path.GetFullPath(variable.Command, config.Directory);
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = variable.Command,
                Arguments = variable.CommandArgs,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                UseShellExecute = false,
                WorkingDirectory = config.Directory,
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

    [GeneratedRegex("\\${(?<var>\\w+)}", RegexOptions.Compiled)]
    private static partial Regex VariableRegex();
}