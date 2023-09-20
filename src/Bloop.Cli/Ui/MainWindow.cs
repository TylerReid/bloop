using Bloop.Core;
using System.Data;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using Terminal.Gui;

namespace Bloop.Cli.Ui;

internal class MainWindow : Toplevel
{
    private List<Config> _configs = new List<Config>();
    private Config? _selectedConfig;
    private Request? _selectedRequest;
    private readonly Blooper _blooper = new();
    private readonly DataTable _scratchVariables = new();

    public FrameView LeftPane { get; set; }
    public ListView RequestListView { get; set; }
    public FrameView RightPane { get; set; }
    public TextView ResultsView { get; set; }
    public StatusBar MainStatusBar { get; set; }
    public StatusItem ProcessingItem { get; set; }
    public StatusBar VariableStatusBar { get; set; }

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
            Height = Dim.Fill(1),
            CanFocus = true,
        };
        RightPane = new FrameView("Results")
        {
            X = Pos.Right(LeftPane),
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(1),
            CanFocus = true,
        };

        RequestListView = new ListView
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
        
        ResultsView = new TextView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            CanFocus = true,
            ColorScheme = new ColorScheme(),
            ReadOnly = true,
        };
        
        RightPane.Add(ResultsView);

        ProcessingItem = new StatusItem(Key.Null, "", () => { });
        MainStatusBar = new StatusBar
        {
            Visible = true,
            CanFocus = false,
            Items = new[]
            {
                new StatusItem(Key.Q | Key.CtrlMask, "~Ctrl-Q~ Quit", RequestStop),
                new StatusItem(Key.V | Key.AltMask, "~Alt-V~ Variables", SwitchToVariableView),
                ProcessingItem,
            },
        };

        VariableStatusBar = new StatusBar
        {
            Visible = true,
            CanFocus = false,
            Items = new[]
            {
                new StatusItem(Key.Q | Key.CtrlMask, "~Ctrl-Q~ Back", SwitchToMainView),
            },
        };

        _scratchVariables.Columns.Add("Name", typeof(string));
        _scratchVariables.Columns.Add("Value", typeof(string));

        SwitchToMainView();

        _ = LoadAsync();
    }

    private void SwitchToMainView()
    {
        foreach (DataRow row in _scratchVariables.Rows)
        {
            _selectedConfig!.Variables
                .First(x => x.Name == (string)row["Name"]).Value = (string?)row["Value"];
        }
        _scratchVariables.Clear();
        RemoveAll();
        Add(LeftPane);
        Add(RightPane);
        Add(MainStatusBar);
    }

    private void SwitchToVariableView()
    {
        if (_selectedConfig == null) { return; }
        RemoveAll();

        var frame = new FrameView("Variables")
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(1),
            CanFocus = true,
        };

        foreach (var variable in _selectedConfig.Variables)
        {
            _scratchVariables.Rows.Add(variable.Name, variable.Value);
        }

        var table = new TableView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            Table = _scratchVariables,
        };

        frame.Add(table);
        // todo add edit dialog for table

        Add(frame);
        Add(VariableStatusBar);
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

        ProcessingItem.Title = ResultsView.Text = "Sending bloop";

        var stopwatch = new Stopwatch();
        stopwatch.Start();
        Directory.SetCurrentDirectory(_selectedConfig.Directory);
        var result = await _blooper.SendRequest(_selectedConfig, _selectedRequest);
        await result.MatchAsync(async response =>
        {
            if (response.Content.Headers.ContentType?.MediaType == "application/json")
            {
                var parsedJson = JsonNode.Parse(await response.Content!.ReadAsStreamAsync());
                var json = parsedJson!.ToJsonString(options: new JsonSerializerOptions
                {
                    WriteIndented = true,
                });
                ResultsView.Text = json;
            }
            else
            {
                ResultsView.Text = await response.Content.ReadAsStringAsync();
            }
        },
        error =>
        {
            ResultsView.Text = error.Message;
            return Task.CompletedTask;
        });
        stopwatch.Stop();
        ProcessingItem.Title = $"Response Time: {stopwatch.Elapsed}";
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
