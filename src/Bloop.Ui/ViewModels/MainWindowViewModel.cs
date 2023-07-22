using AvaloniaEdit.Document;
using Bloop.Avalonia.Ui.Models;
using Bloop.Core;
using ReactiveUI.Fody.Helpers;
using System.Collections.ObjectModel;

namespace Bloop.Avalonia.Ui.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private Blooper _blooper = new();

    [Reactive] public ObservableCollection<Config> Configs { get; set; } = new();
    [Reactive] public Config? BloopConfig { get; set; }
    [Reactive] public Request? SelectedRequest { get ; set; }
    [Reactive] public RequestResult? RequestResult { get; set; }
    [Reactive] public TextDocument? RequestResultDocument { get; set; }

    public MainWindowViewModel()
    {
        Configs = new(ConfigurationLoader.LoadConfigs());
        BloopConfig = Configs.FirstOrDefault();
    }

    public async Task SendRequestAsync()
    {
        await Task.Delay(TimeSpan.FromSeconds(1));
        RequestResultDocument = new TextDocument("derp derp");
    }
}
