using CommandLine;

namespace Bloop.Cli;

[Verb("request", isDefault: true)]
public class RequestOptions
{
    [Value(0, MetaName = "request", HelpText = "name of the request to send")]
    public string RequestName { get; set; } = "";
    [Option("prettyprint", Default = true)]
    public bool PrettyPrint { get; set; } = true;
    [Option('v', "verbose", Default = false)]
    public bool Verbose { get; set; } = false;
    [Option('c', "config-path", Default = "bloop.toml")]
    public string ConfigPath { get; set; } = "bloop.toml";
    [Option('i', "insecure", Default = false, HelpText = "disables certificate validation")]
    public bool Insecure { get; set; } = false;
    [Option("var", HelpText = "variables provided in a `key=value,key2=value2` format", Separator = ',')]
    public IEnumerable<string>? Variables { get; set; }
}

[Verb("list", HelpText = "list requests and variables")]
public class ListOptions
{
    [Value(0, MetaName = "type", HelpText = "type of resource to list")]
    public string Type { get; set; } = "all";
    [Option('c', "config-path", Default = "bloop.toml")]
    public string ConfigPath { get; set; } = "bloop.toml";
}