namespace Bloop.Core;

public class Config
{
    public Dictionary<string, Request> Request { get; set; } = new();
    public Dictionary<string, Variable> Variable { get; set; } = new();
}

public class Request
{
    public Uri Uri { get; set; } = new Uri("http://localhost");
    public HttpMethod Method { get; set; } = HttpMethod.Get;
    public string? Body { get; set; }
    public string? ContentType { get; set; }
    public Dictionary<string, string> Headers { get; set;} = new();
    public List<PostProcess> PostProcess { get; set; } = new();

    public override string ToString() =>  $"{{ Uri: {Uri}, Method: {Method}, Body: {Body}, ContentType: {ContentType} }}";
}

public class PostProcess
{
    public string? Variable { get; set; }
    public string? JPath { get; set; }

    public override string ToString() => $"{{ Variable: {Variable}, JPath: {JPath} }}";
}

public class Variable
{
    public string Source { get; set; } = "";
    public string? Value { get; set; } = "";

    public override string ToString() => $"{{ Source: {Source}, Value: {Value} }}";
}

public class Error
{
    public string Message { get; }
    
    public Error(string message)
    {
        Message = message;
    }
}
