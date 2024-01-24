using System.Runtime.InteropServices;
using System.Text;
using Tomlyn;

namespace Bloop.Core;

public class ConfigLoader
{
    private static readonly char PathListSeparator = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ';' : ':';

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
            
            return Toml.ToModel<SerializationConfig>(content, options: Options()).ToConfig(path);
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

    public static async Task<List<Config>> LoadConfigsAsync()
    {
        var configs = new List<Config>();
        var envPaths = Environment.GetEnvironmentVariable("BLOOP_CONFIG_DIRS");
        foreach (var path in envPaths?.Split(PathListSeparator).ToList() ?? await LoadPathsFromProfileAsync())
        {
            // todo handle errors
            configs.Add((await LoadConfigAsync(path)).UnwrapSuccess());
        }
        return configs;
    }

    private static async Task<List<string>> LoadPathsFromProfileAsync()
    {
        var meta = await LoadMetaConfigAsync();
        return meta.BloopDirectories;
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
            var t when t == typeof(TimeSpan) => TimeSpan.Parse(s),
            _ => null,
        };
    }

    private class SerializationConfig
    {
        public Dictionary<string, Request> Request { get; set; } = new();
        public Dictionary<string, Variable> Variable { get; set; } = new();
        public Defaults Defaults { get; set; } = new();

        public Config ToConfig(string path)
        {
            var config = new Config
            {
                Defaults = Defaults,
                Directory = Path.GetFullPath(path),
            };
            foreach (var (key, value) in Request.OrderBy(x => x.Key))
            {
                value.Name = key;
                config.Requests.Add(value);
            }
            foreach (var (key, value) in Variable.OrderBy(x => x.Key))
            {
                value.Name = key;
                config.Variables.Add(value);
            }
            return config;
        }
    }
}