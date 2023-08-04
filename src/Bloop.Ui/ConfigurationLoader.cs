using System.Runtime.InteropServices;
using Bloop.Core;

namespace Bloop.Ui;

public static class ConfigurationLoader
{
    private static readonly char PathListSeparator = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ';' : ':';

    public static async Task<List<Config>> LoadConfigsAsync()
    {
        var configs = new List<Config>();
        var envPaths = Environment.GetEnvironmentVariable("BLOOP_CONFIG_DIRS");
        foreach (var path in envPaths?.Split(PathListSeparator).ToList() ?? await LoadPathsFromProfileAsync())
        {
            // todo handle errors
            configs.Add((await ConfigLoader.LoadConfigAsync(path)).UnwrapSuccess());   
        }
        return configs;
    }

    private static async Task<List<string>> LoadPathsFromProfileAsync()
    {
        var meta = await ConfigLoader.LoadMetaConfigAsync();
        return meta.BloopDirectories;
    }
}