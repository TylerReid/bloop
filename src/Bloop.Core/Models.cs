namespace Bloop.Core;

public class Config
{
    public Dictionary<string, Request> Request { get; set; } = new();
    public Dictionary<string, Variable> Variable { get; set; } = new();
}

public class Request
{
    public string Uri { get; set; } = "";
    public HttpMethod Method { get; set; } = HttpMethod.Get;
    public string? Body { get; set; }
    public string? ContentType { get; set; }
    public Dictionary<string, string> Headers { get; set;} = new();

    public override string ToString()
    {
        return $"{Uri} :: {Method} :: {Body} :: {ContentType}";
    }
}

public class Variable
{
    public string Name { get; set; } = "";
    public string Jpath { get; set; } = "";
    public string Value { get; set; } = "";
}
