using Tomlyn;

namespace Bloop.Core;

public class ConfigLoader
{
    public static async Task<Config> LoadConfigAsync(string path)
    {
        var content = await File.ReadAllTextAsync(path);
        return Toml.ToModel<Config>(content);
    }
}