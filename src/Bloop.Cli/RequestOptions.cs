namespace Bloop.Cli;

public class RequestOptions
{
    public string RequestName { get; set; } = "";
    public bool PrettyPrint { get; set; } = true;
    public bool Verbose { get; set; } = false;
    public string ConfigPath { get; set; } = "bloop.toml";
}