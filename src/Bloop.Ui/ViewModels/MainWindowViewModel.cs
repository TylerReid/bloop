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

    public async Task SendRequestAsync(Request request)
    {
        var result = await _blooper.SendRequest(BloopConfig, request);
        RequestResultDocument = await result.MatchAsync(async response => 
        {
            RequestResult = new(request, response);
            var content = await response.Content.ReadAsStringAsync();
            return new TextDocument(content);
        }, 
        error => 
        {
            return Task.FromResult(new TextDocument(error.Message));
        });
    }
}
