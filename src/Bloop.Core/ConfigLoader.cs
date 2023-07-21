using System.Text;
using Tomlyn;

namespace Bloop.Core;

public class ConfigLoader
{
    public static async Task<Either<Config, Error>> LoadConfigAsync(string path)
    {
        try
        {
            string content;
            if (Directory.Exists(path))
            {
                var combinedText = new StringBuilder();
                foreach (var file in Directory.EnumerateFiles(path, "*.toml"))
                {
                    combinedText.AppendLine(await File.ReadAllTextAsync(file));
                }
                content = combinedText.ToString();
            }
            else
            {
                content = await File.ReadAllTextAsync(path);
            }
            
            return Toml.ToModel<Config>(content, options: Options());
        }
        catch (FileNotFoundException e)
        {
            return new Error(e.Message);
        }
        catch (Exception e)
        {
            return new Error($"Could not load config file at {path}. {e.Message}");
        }
    }

    public static async Task<MetaConfig> LoadMetaConfigAsync()
    {
        var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var globalConfigFilePath = Path.Join(homeDir, ".bloop.toml");
        if (!File.Exists(globalConfigFilePath))
        {
            return new MetaConfig();
        }

        var content = await File.ReadAllTextAsync(globalConfigFilePath);
        return Toml.ToModel<MetaConfig>(content, options: Options());
    }

    private static TomlModelOptions Options() => new()
    {
        IgnoreMissingProperties = true,
        ConvertToModel = ConvertToModel,
    };

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