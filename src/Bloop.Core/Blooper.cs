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
        var maybeError = await VariableHandler.SatisfyVariables(this, config, request);
        if (maybeError.Unwrap() is Error e)
        {
            return e;
        }

        var httpRequest = new HttpRequestMessage(request.Method, VariableHandler.ExpandVariables(request.Uri, config));

        foreach (var (name, value) in request.Headers)
        {
            httpRequest.Headers.TryAddWithoutValidation(name, VariableHandler.ExpandVariables(value, config));
        }

        if (request.Form != null)
        {
            var form = new Dictionary<string, string>();
            foreach (var (k, v) in request.Form)
            {
                var key = VariableHandler.ExpandVariables(k, config);
                var value = VariableHandler.ExpandVariables(v, config);
                form[key] = value;
            }
            httpRequest.Content = new FormUrlEncodedContent(form);
        } 
        else if (request.Body != null)
        {
            httpRequest.Content = new StringContent(
                VariableHandler.ExpandVariables(request.Body, config), 
                System.Text.Encoding.UTF8, 
                request.ContentType
            );
        }

        var response = await _client.SendAsync(httpRequest);

        var content = await response.Content.ReadAsStringAsync();

        
        if (response.IsSuccessStatusCode)
        {
            var requestName = config.Request.Single(x => x.Value == request).Key;
            var variables = config.Variable.Where(x => x.Value.Source == requestName);

            //maybe support xpath or like direct body response to var?
            if (response.Content?.Headers?.ContentType?.MediaType == "application/json")
            {
                var json = JObject.Parse(content);
                foreach (var (_, variable) in variables)
                {
                    if (variable.Jpath == null)
                    {
                        continue;
                    }

                    var jsonValue = json.SelectToken(variable.Jpath!);
                    if (jsonValue == null)
                    {
                        continue;
                    }
                    variable.Value = jsonValue.ToString();
                }
            }
        }

        return response;
    }

    public async Task<Either<HttpResponseMessage, Error>> SendRequest(Config config, string requestName)
    {
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
}