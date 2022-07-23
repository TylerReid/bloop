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

    public static List<string> GetVariables(Request request) => request.Headers.Values.SelectMany(GetVariables)
        .Concat(GetVariables(request.Body ?? ""))
        .Concat(GetVariables(request.Uri))
        .Distinct()
        .ToList();

    public static string ExpandVariables(string original, Config config) => 
        ExpandVariables(original, config.Variable.ToDictionary(x => x.Key, x => x.Value.Value!));

    public static string ExpandVariables(string original, Dictionary<string, string> mappings)
    {
        foreach (var (name, value) in mappings)
        {
            original = original.Replace($"${{{name}}}", value);
        }
        return original;
    }

    public static Dictionary<string, Variable> UnsatisfiedVariables(Config config) => config.Variable
        .Where(x => x.Value.Value == null)
        .ToDictionary(k => k.Key, v => v.Value);

    public static Dictionary<string, Variable> SatisfiedVariables(Config config) => config.Variable
        .Where(x => x.Value.Value is string)
        .ToDictionary(k => k.Key, v => v.Value);

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
}