using Newtonsoft.Json.Linq;
using System.Web;

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

        var urlParts = request.Uri.Split('?', 2);
        var uri = new UriBuilder(VariableHandler.ExpandVariables(urlParts[0], config)!);
        // first expand variables that were included in the request uri
        if (urlParts.Length == 2)
        {
            uri.Query = VariableHandler.ExpandVariables(urlParts[1], config, HttpUtility.UrlEncode);
        }
        // now expand variables that are included in the request query property
        var query = HttpUtility.ParseQueryString(uri.Query);
        foreach (var (k, v) in request.Query)
        {
            var encodedKey = VariableHandler.ExpandVariables(k, config);
            var encodedValue = VariableHandler.ExpandVariables(v, config);
            query.Add(encodedKey, encodedValue);
        }

        uri.Query = query.ToString();
        
        var httpRequest = new HttpRequestMessage(request.Method, uri.Uri);

        foreach (var (name, value) in request.Headers)
        {
            httpRequest.Headers.TryAddWithoutValidation(name, VariableHandler.ExpandVariables(value, config));
        }

        foreach (var (name, value) in config.Defaults.Headers)
        {
            if (httpRequest.Headers.All(x => x.Key != name))
            {
                httpRequest.Headers.TryAddWithoutValidation(name, VariableHandler.ExpandVariables(value, config));
            }
        }

        if (request.Form != null)
        {
            var form = new Dictionary<string, string?>();
            foreach (var (k, v) in request.Form)
            {
                var key = VariableHandler.ExpandVariables(k, config);
                var value = VariableHandler.ExpandVariables(v, config);
                if (key != null)
                {
                    form[key] = value;
                }
            }
            httpRequest.Content = new FormUrlEncodedContent(form);
        } 
        else if (request.Body != null)
        {
            httpRequest.Content = new StringContent(
                VariableHandler.ExpandVariables(request.Body, config) ?? "", 
                System.Text.Encoding.UTF8, 
                request.ContentType ?? "application/json"
            );
        }

        HttpResponseMessage response;
        try
        {
            response = await _client.SendAsync(httpRequest);
        }
        catch (Exception ex)
        {
            return new Error(ex.Message);
        }
        var content = await response.Content.ReadAsStringAsync();

        if (response.IsSuccessStatusCode)
        {
            var variables = config.Variables.Where(x => x.Source == request.Name);

            //maybe support xpath or like direct body response to var?
            if (response.Content?.Headers?.ContentType?.MediaType == "application/json")
            {
                var json = JToken.Parse(content);
                foreach (var variable in variables)
                {
                    if (variable.Jpath == null)
                    {
                        continue;
                    }

                    var jsonValue = json.SelectToken(variable.Jpath);
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
        var request = config.Requests.SingleOrDefault(x => x.Name == requestName);
        if (request == null)
        {
            return new Error($"no request found with name `{requestName}` in config");
        }
        return request;
    }
}