using Bloop.Core;
using Terminal.Gui;

namespace Bloop.Cli.Ui;

internal class MainWindow : Toplevel
{
    private List<Config> _configs = new List<Config>();
    private Config? _selectedConfig;
    private Request? _selectedRequest;
    private readonly Blooper _blooper = new();

    public FrameView LeftPane { get; set; }
    public ListView RequestListView { get; set; }
    public FrameView RightPane { get; set; }

    public MainWindow()
    {
        X = 0;
        Y = 0;
        Width = Dim.Fill();
        Height = Dim.Fill();

        LeftPane = new FrameView("Requests") 
        {
            X = 0, 
            Y = 0,
            Width = Dim.Percent(25),
            Height = Dim.Fill(),
            CanFocus = true,
        };
        RightPane = new FrameView("Results")
        {
            X = Pos.Right(LeftPane),
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            CanFocus = true,
        };

        RequestListView = new ListView(new string[0])
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            AllowsMarking = false,
        };

        RequestListView.SelectedItemChanged += RequestSelectionChanged;
        RequestListView.KeyPress += RequestListKeyPressed;

        LeftPane.Add(RequestListView);

        Add(LeftPane);
        Add(RightPane);

        _ = LoadAsync();
    }

    private void RequestListKeyPressed(KeyEventEventArgs args)
    {
        if (args.KeyEvent.Key == Key.Enter)
        {
            _ = SendSelectedRequest();
        }
    }

    private async Task SendSelectedRequest()
    {
        if (_selectedConfig == null || _selectedRequest == null) { return; }

        Directory.SetCurrentDirectory(_selectedConfig.Directory);
        var result = await _blooper.SendRequest(_selectedConfig, _selectedRequest);
        result.MatchAsync(async response => 
        {
            RightPane.Text = await response.Content.ReadAsStringAsync();
        }, 
        error => 
        {
            RightPane.Text = error.Message;
            return Task.CompletedTask;
        });
    }

    private void RequestSelectionChanged(ListViewItemEventArgs args)
    {
        _selectedRequest = _selectedConfig!.Requests[args.Item];
    }

    private async Task LoadAsync()
    {
        _configs = await ConfigLoader.LoadConfigsAsync();
        SelectConfig(_configs.FirstOrDefault());
    }

    private void SelectConfig(Config? config)
    {
        _selectedConfig = config;
        if (config == null) { return; }
        RequestListView.SetSource(config.Requests.Select(x => x.Name).ToList());
    }
}
