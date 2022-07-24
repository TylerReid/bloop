using System.Linq.Expressions;

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

    public override string ToString()
    {
        var s = $"{{ Uri: {Uri}, Method: {Method}";
        s = ModelHelper.AppendIfValue(s, () => Body);
        s = ModelHelper.AppendIfValue(s, () => ContentType);
        return s + " }";
    }
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

    public override string ToString()
    {
        var s = "{ ";
        s = ModelHelper.AppendIfValue(s, () => Source);
        s = ModelHelper.AppendIfValue(s, () => Value);
        s = ModelHelper.AppendIfValue(s, () => Jpath);
        s = ModelHelper.AppendIfValue(s, () => Command);
        s = ModelHelper.AppendIfValue(s, () => CommandArgs);
        s = ModelHelper.AppendIfValue(s, () => File);
        s = ModelHelper.AppendIfValue(s, () => Env);
        return s + " }";
    }
}

internal class ModelHelper
{
    // this is dumb but fun
    public static string AppendIfValue(string s, Expression<Func<object?>> expression)
    {
        var value = expression.Compile()();
        if (value != null)
        {
            s += $"{(s.Contains(":") ? "," : "")} {(expression.Body as MemberExpression)?.Member?.Name}: {value}";
        }
        return s;
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
