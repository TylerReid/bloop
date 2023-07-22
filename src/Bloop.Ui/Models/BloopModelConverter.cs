using Bloop.Core;

namespace Bloop.Avalonia.Ui.Models;

class BloopModelConverter
{
    public static UiConfig Convert(Config config)
    {
        var newConfig = new UiConfig();

        foreach (var (name, request) in config.Request) 
        {
            newConfig.Requests.Add(new NamedObject<Request>
            {
                Name = name,
                Value = request,
            });
        }

        foreach (var (name, variable) in config.Variable)
        {
            newConfig.Variables.Add(new NamedObject<Variable>
            {
                Name = name,
                Value = variable,
            });
        }

        newConfig.Defaults = config.Defaults;

        return newConfig;
    }
}
