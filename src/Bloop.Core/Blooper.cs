using System.Text;

namespace Bloop.Core;

public class Blooper
{
    private readonly HttpClient _client = new();

    public async Task<Either<HttpResponseMessage, Error>> SendRequest(Config config, string requestName)
    {
        
        if (!config.Request.TryGetValue(requestName, out var request))
        {
            return new Error
            {
                Message = $"no request found with name `{requestName}` in config",
            };
        }

        var output = new StringBuilder();
        
        var httpRequest = new HttpRequestMessage(request.Method, request.Uri);
        foreach (var (name, value) in request.Headers) 
        {
            httpRequest.Headers.TryAddWithoutValidation(name, value);
        }

        if (request.Body is string)
        {
            httpRequest.Content = new StringContent(request.Body, System.Text.Encoding.UTF8, request.ContentType);
        }

        return await _client.SendAsync(httpRequest);
    }
}