using CommandLine;

namespace Bloop.Cli;

public abstract class BaseOptions
{
    [Option('c', "config-path", HelpText = "directory or file to load configs from")]
    public string? ConfigPath { get; set; }
    [Option("no-color")]
    public bool NoColor { get; set; }
}

[Verb("request", isDefault: true)]
public class RequestOptions : BaseOptions
{
    [Value(0, MetaName = "request", HelpText = "name of the request to send")]
    public string RequestName { get; set; } = "";
    [Option("pretty-print", Default = true)]
    public bool PrettyPrint { get; set; } = true;
    [Option('v', "verbose", Default = false)]
    public bool Verbose { get; set; } = false;
    [Option('i', "insecure", Default = false, HelpText = "disables certificate validation")]
    public bool Insecure { get; set; } = false;
    [Option("var", HelpText = "variables provided in a `key=value,key2=value2` format", Separator = ',')]
    public IEnumerable<string>? Variables { get; set; }
}

[Verb("list", HelpText = "list requests and variables")]
public class ListOptions : BaseOptions
{
    [Value(0, MetaName = "type", HelpText = "type of resource to list")]
    public string Type { get; set; } = "all";
}

[Verb("validate", HelpText = "validate configuration")]
public class ValidateOptions : BaseOptions
{
}