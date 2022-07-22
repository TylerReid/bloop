using Newtonsoft.Json.Linq;

namespace Bloop.Core;

public class Blooper
{
    private readonly HttpClient _client;

    public Blooper() : this(new()) {}
    public Blooper(HttpClient client)
    {
        _client = client;
    }

    public async Task<Either<HttpResponseMessage, Error>> SendRequest(Config config, Request request)
    {
        var httpRequest = new HttpRequestMessage(request.Method, request.Uri);

        var maybeError = await SatisfyVariables(config, request);

        if (maybeError.Unwrap() is Error e)
        {
            return e;
        }

        foreach (var (name, value) in request.Headers)
        {
            httpRequest.Headers.TryAddWithoutValidation(name, VariableHandler.ExpandVariables(value, config));
        }

        if (request.Body is string)
        {
            httpRequest.Content = new StringContent(
                VariableHandler.ExpandVariables(request.Body, config), 
                System.Text.Encoding.UTF8, 
                request.ContentType
            );
        }

        var response = await _client.SendAsync(httpRequest);

        var content = await response.Content.ReadAsStringAsync();

        //maybe support xpath or like direct body response to var?
        var jsonPostProcess = request.PostProcess
            .Where(x => x.Variable is string && x.Jpath is string)
            .ToList();

        if (response.IsSuccessStatusCode && jsonPostProcess.Any()) {
            var json = JObject.Parse(content);

            foreach (var postprocess in jsonPostProcess)
            {
                var jsonValue = json.SelectToken(postprocess.Jpath!);
                if (jsonValue == null)
                {
                    continue;
                }
                if (!config.Variable.ContainsKey(postprocess.Variable))
                {
                    return new Error($"variable named {postprocess.Variable} is used in a post process step, but is not defined as a variable");
                }
                config.Variable[postprocess.Variable].Value = jsonValue.ToString();
            }
        }

        return response;
    }

    public async Task<Either<HttpResponseMessage, Error>> SendRequest(Config config, string requestName)
    {
        //todo replace with MatchAsync when the real Either gets open sourced
        return GetRequest(config, requestName).Unwrap() switch {
            Request r => await SendRequest(config, r),
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

    public async Task<Either<Unit, Error>> SatisfyVariables(Config config, Request request)
    {
        //todo document why this works
        //todo infinite loop protection?
        var variables = VariableHandler.GetVariables(request);
        foreach (var v in variables)
        {
            if (config.Variable.TryGetValue(v, out var var))
            {
                if (var.Value != null)
                {
                    continue;
                }

                var response = await SendRequest(config, var.Source);
                if (response.Unwrap() is Error e)
                {
                    return e;
                }
            }
            else
            {
                return new Error($"variable {v} is used in request {request.Uri} but is not defined as a variable");
            }
        }

        return Unit.Instance;
    }
}