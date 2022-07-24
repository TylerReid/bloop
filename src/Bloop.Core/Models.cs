using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;

namespace Bloop.Core;

public class Config
{
    public Dictionary<string, Request> Request { get; set; } = new();
    public Dictionary<string, Variable> Variable { get; set; } = new();
}

public class Request
{
    public string Uri { get; set; } = "http://localhost";
    public HttpMethod Method { get; set; } = HttpMethod.Get;
    public string? Body { get; set; }
    public string? ContentType { get; set; }
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

    public override string ToString() => ModelHelper.ToString(this);
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
                sb.Append($"{comma} {property.Name}: {value}");
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
