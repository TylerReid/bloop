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
    public List<PostProcess> PostProcess { get; set; } = new();

    public override string ToString()
    {
        var s = $"{{ Uri: {Uri}, Method: {Method}";
        if (Body is string)
        {
            s += $", Body: {Body}";
        }
        if (ContentType is string)
        {
            s += $", ContentType: {ContentType}";
        }
        s += " }";
        foreach (var pp in PostProcess)
        {
            s += $"\n\t{pp}";
        }
        return s;
    }
}

public class PostProcess
{
    public string Variable { get; set; } = "";
    public string Jpath { get; set; } = "";

    public override string ToString() => $"{{ Variable: {Variable}, JPath: {Jpath} }}";
}

public class Variable
{
    public string Source { get; set; } = "";
    public string? Value { get; set; }

    public override string ToString()
    {
        var s = $"{{ Source: {Source}";
        if (Value is string)
        {
            s += $", Value: {Value}";
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
