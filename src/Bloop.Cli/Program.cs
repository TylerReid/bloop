using Bloop.Core;

var config = await ConfigLoader.LoadConfigAsync("bloop.toml");

foreach (var (name, request) in config.Request)
{
    Console.WriteLine($"{name} {request}");
    foreach (var pp in request.PostProcess)
    {
        Console.WriteLine($"\t{pp}");
    }
}

foreach (var (name, var) in config.Variable)
{
    Console.WriteLine($"{name} {var}");
}

var blooper = new Blooper();
await blooper.Bloop(config, "google");
