using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Bloop.Core;

namespace Bloop.Avalonia.Ui;

public class ConfigurationLoader
{
    private static readonly char PathListSeparator = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ';' : ':';

    // todo remove this once async vm load is figured out
    [Obsolete]
    public static List<Config> LoadConfigs()
    {
        var configs = new List<Config>();
        var envPaths = Environment.GetEnvironmentVariable("BLOOP_CONFIG_DIRS");
        foreach (var path in envPaths?.Split(PathListSeparator).ToList() ?? LoadPathsFromProfile())
        {
            // todo handle errors
            configs.Add((ConfigLoader.LoadConfig(path)).UnwrapSuccess());
        }
        return configs;
    }

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

    private static List<string> LoadPathsFromProfile()
    {
        var meta = ConfigLoader.LoadMetaConfig();
        return meta.BloopDirectories;
    }
}