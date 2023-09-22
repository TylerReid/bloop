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
        // doing a url encode is not guaranteed to be correct because you could build any part of the url
        // so ${baseUrl} might legitimately be user:pw@example.org so encoding that breaks things
        // but this seems less likely that query params so break for now. Try and find a better solution later
        var httpRequest = new HttpRequestMessage(request.Method, 
            VariableHandler.ExpandVariables(request.Uri, config, HttpUtility.UrlEncode));

        foreach (var (name, value) in request.Headers)
        {
            httpRequest.Headers.TryAddWithoutValidation(name, VariableHandler.ExpandVariables(value, config));
        }

        foreach (var (name, value) in config.Defaults.Headers)
        {
            if (!httpRequest.Headers.Any(x => x.Key == name))
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
        var request = config.Requests.SingleOrDefault(x => x.Name == requestName);
        if (request == null)
        {
            return new Error($"no request found with name `{requestName}` in config");
        }
        return request;
    }
}