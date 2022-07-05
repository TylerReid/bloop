namespace Bloop.Core;

public class Blooper
{
    private readonly HttpClient _client = new();

    public async Task Bloop(Config config, string requestName)
    {
        if (!config.Request.TryGetValue(requestName, out var request))
        {
            //todo: don't do this, have return type
            throw new Exception("ahhhh");
        }
        
        var httpRequest = new HttpRequestMessage(request.Method, request.Uri);
        foreach (var (name, value) in request.Headers) 
        {
            httpRequest.Headers.TryAddWithoutValidation(name, value);
        }

        if (request.Body is string)
        {
            httpRequest.Content = new StringContent(request.Body, System.Text.Encoding.UTF8, request.ContentType);
        }

        var response = await _client.SendAsync(httpRequest);

        Console.WriteLine($"response {response.StatusCode}");
    }
}