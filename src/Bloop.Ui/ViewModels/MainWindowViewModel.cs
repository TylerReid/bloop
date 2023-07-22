using Bloop.Avalonia.Ui.Models;
using Bloop.Core;
using ReactiveUI.Fody.Helpers;
using System.Collections.ObjectModel;

namespace Bloop.Avalonia.Ui.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    [Reactive] public ObservableCollection<UiConfig> Configs { get; set; } = new();
    [Reactive] public UiConfig? BloopConfig { get; set; }
    [Reactive] public NamedObject<Request>? SelectedRequest { get ; set; }

    public MainWindowViewModel()
    {
        Configs = new(ConfigurationLoader.LoadConfigs());
        BloopConfig = Configs.FirstOrDefault();
    }
}
