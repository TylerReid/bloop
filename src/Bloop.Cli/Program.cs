using Bloop.Core;
using System.Text.Json;
using System.Text.Json.Nodes;

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

var r = await blooper.SendRequest(config, "google");

r.Match(
    response => {
        Console.WriteLine($"status was {response.StatusCode} for {response.RequestMessage?.RequestUri}");
    },
    error => Console.WriteLine(error)
);

r = await blooper.SendRequest(config, "not real");
r.Match(
    _ => Console.WriteLine("did not expect to get here"),
    error => Console.WriteLine(error.Message)
);

r = await blooper.SendRequest(config, "somejson");
if (r.Unwrap() is HttpResponseMessage message)
{
    await PrintResponse(message, new RequestOptions
    {
        PrettyPrint = true,
        Verbose = true,
    });
}


async Task PrintResponse(HttpResponseMessage response, RequestOptions options)
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

public class RequestOptions
{
    public string RequestName { get; set; } = "";
    public bool PrettyPrint { get; set; } = true;
    public bool Verbose { get; set; } = false;
}