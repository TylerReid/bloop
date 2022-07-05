using Tomlyn;
using Tomlyn.Model;

namespace Bloop.Core;

public class ConfigLoader
{

    public static async Task<Config> LoadConfigAsync(string path)
    {
        var content = await File.ReadAllTextAsync(path);
        return Toml.ToModel<Config>(content, options: new TomlModelOptions
        {
            IgnoreMissingProperties = true,
            ConvertToModel = ConvertToModel,
        });
    }

    private static object? ConvertToModel(object value, Type type)
    {
        if (value is not string s) {
            return null;
        }
        return type switch {
            var t when t == typeof(Uri) => new Uri(s),
            var t when t == typeof(HttpMethod) => new HttpMethod(s),
            _ => null,
        };
    }
}