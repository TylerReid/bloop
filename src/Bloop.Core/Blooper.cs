using System.Text;

namespace Bloop.Core;

public class Blooper
{
    private readonly HttpClient _client;

    public Blooper() : this(new()) {}
    public Blooper(HttpClient client)
    {
        _client = client;
    }

    public async Task<Either<HttpResponseMessage, Error>> SendRequest(Request request)
    {
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

    public async Task<Either<HttpResponseMessage, Error>> SendRequest(Config config, string requestName)
    {
        //todo replace with MatchAsync when the real Either gets open sourced
        return GetRequest(config, requestName).Unwrap() switch {
            Request r => await SendRequest(r),
            Error e => e,
            _ => new Error(""),
        };
    }

    public Either<Request, Error> GetRequest(Config config, string requestName)
    {
        if (!config.Request.TryGetValue(requestName, out var request))
        {
            return new Error($"no request found with name `{requestName}` in config");
        }
        return request;
    }
}