using System.Text;

namespace Bloop.Core;

public class Config
{
    public Dictionary<string, Request> Request { get; set; } = new();
    public Dictionary<string, Variable> Variable { get; set; } = new();
    public Defaults Defaults { get; set; } = new();
}

public class Request
{
    public string Uri { get; set; } = "http://localhost";
    public HttpMethod Method { get; set; } = HttpMethod.Get;
    public string? Body { get; set; }
    public string? ContentType { get; set; }
    public Dictionary<string, string>? Form { get; set; }
    public Dictionary<string, string> Headers { get; set;} = new();

    public override string ToString() => ModelHelper.ToString(this);
}

public class Variable
{
    public string? Source { get; set; }
    public string? Value { get; set; }
    public string? Jpath { get; set; }
    public string? Command { get; set; }
    public string? CommandArgs { get; set; }
    public string? File { get; set; }
    public string? Env { get; set; }
    public string? Default { get; set; }

    public override string ToString() => ModelHelper.ToString(this);
}

public class Defaults
{
    public Dictionary<string, string> Headers { get; set; } = new();
}

internal class ModelHelper
{
    // this is dumb but fun
    public static string ToString<T>(T obj)
    {
        var sb = new StringBuilder();
        sb.Append("{ ");

        var comma = "";
        var type = typeof(T);
        foreach (var property in type.GetProperties())
        {
            var value = property.GetValue(obj);
            if (value == null)
            {
                continue;
            }
            if (value is Dictionary<string, string> d)
            {
                //todo print these? might be too much info
            }
            else
            {
                var valueString = value.ToString();
                // if the value is too big, like a request body, don't include it in output
                if (valueString?.Length > 100)
                {
                    var denewlined = valueString
                        .Replace("\n", "")
                        .Replace("\r", "");
                    valueString = $"{denewlined.Substring(0, 15)}...";
                }
                sb.Append($"{comma} {property.Name}: {valueString}");
            }
            comma = ",";
        }

        sb.Append(" }");
        return sb.ToString();
    }
}

public class Error
{
    public string Message { get; }
    
    public Error(string message)
    {
        Message = message;
    }
}
