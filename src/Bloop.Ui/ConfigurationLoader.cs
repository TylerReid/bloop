using System.Runtime.InteropServices;
using Bloop.Avalonia.Ui.Models;
using Bloop.Core;

namespace Bloop.Avalonia.Ui;

public static class ConfigurationLoader
{
    private static readonly char PathListSeparator = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ';' : ':';

    // todo remove this once async vm load is figured out
    [Obsolete]
    public static List<UiConfig> LoadConfigs()
    {
        var configs = new List<UiConfig>();
        var envPaths = Environment.GetEnvironmentVariable("BLOOP_CONFIG_DIRS");
        foreach (var path in envPaths?.Split(PathListSeparator).ToList() ?? LoadPathsFromProfile())
        {
            // todo handle errors
            var coreConfig = ConfigLoader.LoadConfig(path).UnwrapSuccess().ToCoreConfig();
            configs.Add(coreConfig);
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

    public static UiConfig ToCoreConfig(this Config config)
    {
        var uiConfig = new UiConfig
        {
            Defaults = config.Defaults
        };
        foreach (var (key, request) in config.Request)
        {
            uiConfig.Requests.Add(new NamedObject<Request> { Name = key, Value = request, });
        }
        foreach (var (key, variable) in config.Variable)
        {
            uiConfig.Variables.Add(new NamedObject<Variable> { Name = key, Value = variable, });
        }
        return uiConfig;
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