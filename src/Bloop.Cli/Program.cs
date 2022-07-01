using Bloop.Core;

var config = await ConfigLoader.LoadConfigAsync("bloop.toml");

foreach (var (name, request) in config.Request)
{
    Console.WriteLine($"{name} {request}");
}
