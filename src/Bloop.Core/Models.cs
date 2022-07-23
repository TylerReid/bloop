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
        if (Body != null)
        {
            s += $", Body: {Body}";
        }
        if (ContentType != null)
        {
            s += $", ContentType: {ContentType}";
        }
        return s + " }";
    }
}

public class Variable
{
    public string Source { get; set; } = "";
    public string? Value { get; set; }
    public string? Jpath { get; set; }
    public string? Command { get; set; }
    public string? CommandArgs { get; set; }

    public override string ToString()
    {
        var s = $"{{ Source: {Source}";
        if (Value != null)
        {
            s += $", Value: {Value}";
        }
        if (Jpath != null)
        {
            s += $", Jpath: {Jpath}";
        }
        if (Command != null)
        {
            s += $", Command: {Command}";
        }
        if (CommandArgs != null)
        {
            s += $", CommandArgs: {CommandArgs}";
        }
        return s + " }";
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
