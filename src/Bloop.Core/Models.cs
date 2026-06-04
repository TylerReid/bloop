using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Bloop.Core;

public record Config : BaseModel
{
    [IgnoreDataMember]
    public string Directory { get; set; } = "";
    [IgnoreDataMember]
    public string? Env { get; set; }
    public List<Request> Requests { get; set; } = new();
    public List<Variable> Variables { get; set; } = new();
    public Defaults Defaults { get; set; } = new();

    public override string ToString() => base.ToString();
}

public record Request : BaseModel
{
    [IgnoreDataMember]
    public string Name { get; set; } = "";
    public string Uri { get; set; } = "http://localhost";
    public HttpMethod Method { get; set; } = HttpMethod.Get;
    public string? Body { get; set; }
    public string? ContentType { get; set; }
    public Dictionary<string, string>? Form { get; set; }
    public Dictionary<string, string> Headers { get; set; } = new();
    public Dictionary<string, string> Query { get; set; } = new();
    
    public override string ToString() => base.ToString();

    public string ToHttpString()
    {
        return $"""
            {Method} {Uri}
            {Headers.Aggregate("", (acc, kv) => acc + $"{kv.Key}: {kv.Value}\n").TrimEnd("\n")}
            {BodyString()}
            """;
    }

    public static Request FromHttpString(string http)
    {
        var lines = http.Split('\n');

        var firstLine = lines[0].Trim();
        var spaceIndex = firstLine.IndexOf(' ');
        var method = spaceIndex >= 0 ? new HttpMethod(firstLine[..spaceIndex]) : HttpMethod.Get;
        var uri = spaceIndex >= 0 ? firstLine[(spaceIndex + 1)..].Trim() : firstLine;

        var headers = new Dictionary<string, string>();
        var bodyParts = new List<string>();
        var inBody = false;

        for (var i = 1; i < lines.Length; i++)
        {
            var line = lines[i];
            if (!inBody)
            {
                var colonIndex = line.IndexOf(": ");
                if (colonIndex > 0 && line[..colonIndex].All(c => char.IsLetterOrDigit(c) || c == '-'))
                {
                    headers[line[..colonIndex].Trim()] = line[(colonIndex + 2)..].Trim();
                    continue;
                }
                inBody = true;
            }
            bodyParts.Add(line);
        }

        var body = string.Join('\n', bodyParts).Trim();

        return new Request
        {
            Method = method,
            Uri = uri,
            Headers = headers,
            Body = body.Length > 0 ? body : null,
        };
    }

    private string? BodyString()
    {
        if (Form is not null)
        {
            return Form.Aggregate("", (acc, kv) => acc + $"{kv.Key}={kv.Value}&").TrimEnd('&');
        }
        return Body;
    }
}

[DebuggerDisplay("{Name}")]
public record Variable : BaseModel
{
    [IgnoreDataMember]
    public string Name { get; set; } = "";
    public string? Source { get; set; }
    private string? _value;
    public string? Value 
    { 
        get => _value; 
        set
        {
            _value = value;
            ValueDateTime = DateTime.UtcNow;
            OnPropertyChanged();
        } 
    }
    public string? Jpath { get; set; }
    public string? Command { get; set; }
    public string? CommandArgs { get; set; }
    public string? File { get; set; }
    public string? Env { get; set; }
    public string? Default { get; set; }
    public TimeSpan? ValueLifetime { get; set; }
    [IgnoreDataMember]
    public DateTime? ValueDateTime { get; set; }
    public Dictionary<string, Variable> VariableSets { get; set; } = new();
    [IgnoreDataMember]
    public string? SatisfiedEnv { get; set; }

    public bool IsExpired() => ValueLifetime.HasValue 
            && ValueDateTime.HasValue 
            && ValueDateTime.Value.Add(ValueLifetime.Value) < DateTime.UtcNow;

    public void ClearIfExpired() => Value = IsExpired() ? null : Value;
    
    public override string ToString() => base.ToString();
}

public record Defaults : BaseModel
{
    public Dictionary<string, string> Headers { get; set; } = new();
    
    public override string ToString() => base.ToString();
}

public record MetaConfig : BaseModel
{
    public List<string> BloopDirectories { get; set; } = new();
    
    public override string ToString() => base.ToString();
}

public class Error
{
    public string Message { get; }
    
    public Error(string message)
    {
        Message = message;
    }
}

public record BaseModel : INotifyPropertyChanged
{
    [IgnoreDataMember]
    [JsonIgnore]
    public string Toml => Tomlyn.Toml.FromModel(this);
    [IgnoreDataMember]
    [JsonIgnore]
    public string Json => JsonSerializer.Serialize(this, GetType(), new JsonSerializerOptions { WriteIndented = true, });
    
    public event PropertyChangedEventHandler? PropertyChanged;
    
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    // this is dumb but fun
    public override string ToString()
    {
        var sb = new StringBuilder();
        foreach (var property in GetType().GetProperties()
                     .Where(p => p.CustomAttributes.All(a => a.AttributeType != typeof(IgnoreDataMemberAttribute))))
        {
            var value = property.GetValue(this);
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
                sb.Append($"{property.Name} = {valueString}\n");
            }
        }
        return sb.ToString();
    }
}
