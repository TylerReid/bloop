using Bloop.Core;
using System.Data;
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
    public StatusBar MainStatusBar { get; set; }
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

        MainStatusBar = new StatusBar
        {
            Visible = true,
            CanFocus = false,
            Items = new[]
            {
                new StatusItem(Key.Q | Key.CtrlMask, "~Ctrl-Q~ Quit", RequestStop),
                new StatusItem(Key.V | Key.AltMask, "~Alt-V~ Variables", SwitchToVariableView),
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

        Directory.SetCurrentDirectory(_selectedConfig.Directory);
        var result = await _blooper.SendRequest(_selectedConfig, _selectedRequest);
        _ = result.MatchAsync(async response =>
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
