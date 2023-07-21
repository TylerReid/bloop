using Bloop.Core;
using ReactiveUI.Fody.Helpers;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Bloop.Avalonia.Ui.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    [Reactive] public ObservableCollection<Config> Configs { get; set; } = new();
    [Reactive] public Config? BloopConfig { get; set; }
    // todo if this aren't reactive does this work?
    public Dictionary<string, Request>? Requests => BloopConfig?.Request;
    public Dictionary<string, Variable>? Variables => BloopConfig?.Variable;
    [Reactive] public KeyValuePair<string, Request>? SelectedRequest { get ; set; }

    public MainWindowViewModel()
    {
        Configs = new(ConfigurationLoader.LoadConfigs());
        BloopConfig = Configs.FirstOrDefault();
    }
}
