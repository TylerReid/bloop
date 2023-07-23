using AvaloniaEdit.Document;
using AvaloniaEdit.Highlighting;
using Bloop.Avalonia.Ui.Models;
using Bloop.Avalonia.Ui.Resources;
using Bloop.Core;
using ReactiveUI.Fody.Helpers;
using System.Collections.ObjectModel;
using System.Net.Http.Json;
using System.Text.Json;

namespace Bloop.Avalonia.Ui.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private Blooper _blooper = new();

    [Reactive] public ObservableCollection<Config> Configs { get; set; } = new();
    [Reactive] public Config? BloopConfig { get; set; }
    [Reactive] public Request? SelectedRequest { get ; set; }
    [Reactive] public RequestResult? RequestResult { get; set; }
    [Reactive] public TextDocument? RequestResultDocument { get; set; }
    [Reactive] public IHighlightingDefinition SyntaxHighlighting { get; set; }

    public MainWindowViewModel()
    {
        SetHighlighting(".txt");
        _ = LoadAsync();
    }

    public async Task SendRequestAsync(Request request)
    {
        // this is kind of dumb to set here, but we need to do this to make relative paths from variables work
        // this could be set when we get a new selected config but that would make the auto property go away
        Directory.SetCurrentDirectory(BloopConfig!.Directory);
        var result = await _blooper.SendRequest(BloopConfig!, request);
        RequestResultDocument = await result.MatchAsync(async response => 
        {
            RequestResult = new(request, response);
            SetHighlighting("json");
            return await CreateDocument(response);
        }, 
        error => 
        {
            SetHighlighting("txt");
            return Task.FromResult(new TextDocument(error.Message));
        });
    }

    private async Task LoadAsync()
    {
        Configs = new(await ConfigurationLoader.LoadConfigsAsync());
        BloopConfig = Configs.FirstOrDefault();
    }

    private async Task<TextDocument> CreateDocument(HttpResponseMessage response)
    {
        string content;

        if (response.Content.Headers.ContentType?.MediaType == "application/json")
        {
            var o = await response.Content.ReadFromJsonAsync<dynamic>();
            content = JsonSerializer.Serialize(o, new JsonSerializerOptions 
            { 
                WriteIndented = true,
            });
        }
        else
        {
            content = await response.Content.ReadAsStringAsync();
        }

        return new TextDocument(content);
    }

    private void SetHighlighting(string language)
    {
        SyntaxHighlighting = language switch
        {
            "json" => HighlightingManager.Instance.GetBloopDefinition(language),
            _ => HighlightingManager.Instance.GetDefinition(language),
        };
    }
}
