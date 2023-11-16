using Bloop.Core;
using System.Data;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Terminal.Gui;
using Terminal.Gui.Graphs;

namespace Bloop.Cli.Ui;

internal class MainWindow : Toplevel
{
    private List<Config> _configs = new List<Config>();
    private Config? _selectedConfig;
    private Request? _selectedRequest;
    private readonly Blooper _blooper = new();
    private readonly DataTable _scratchVariables = new();

    private FrameView LeftPane { get; set; }
    private ListView RequestListView { get; set; }
    private FrameView RightPane { get; set; }
    private TextView ResultDetails { get; set; }
    private TextView ResultsView { get; set; }
    private StatusBar MainStatusBar { get; set; }
    private StatusItem ProcessingItem { get; set; }
    private StatusBar VariableStatusBar { get; set; }
    private TableView? VariableTableView { get; set; }

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
        RequestListView.MouseClick += RequestListClick;

        LeftPane.Add(RequestListView);

        ResultDetails = new TextView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Percent(10),
            CanFocus = true,
            ColorScheme = new ColorScheme(),
            ReadOnly = true,
        };

        var line = new LineView
        {
            X = 0,
            Y = Pos.Bottom(ResultDetails),
            Orientation = Orientation.Horizontal,
        };
        
        ResultsView = new TextView
        {
            X = 0,
            Y = Pos.Bottom(line),
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            CanFocus = true,
            ColorScheme = new ColorScheme(),
            ReadOnly = true,
        };

        RightPane.Add(ResultDetails);
        RightPane.Add(line);
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
                new StatusItem(Key.C | Key.CtrlMask, "~Ctrl-C~ Copy Result", CopyResultToClipboard),
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

    private void CopyResultToClipboard()
    {
        if (ResultsView.Text.ToString() != null)
        {
            Clipboard.TrySetClipboardData(ResultsView.Text.ToString());
        }
    }

    private void SwitchToMainView()
    {
        foreach (DataRow row in _scratchVariables.Rows)
        {
            _selectedConfig!.Variables
                .First(x => x.Name == (string)row["Name"]).Value = row["Value"] as string;
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

        VariableTableView = new TableView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            Table = _scratchVariables,
        };

        VariableTableView.CellActivated += EditCurrentCell;

        frame.Add(VariableTableView);
        Add(frame);
        Add(VariableStatusBar);
    }

    private void EditCurrentCell(TableView.CellActivatedEventArgs e)
    {
        if (e.Table == null || e.Col != 1) { return; }
        var oldValue = e.Table.Rows[e.Row][e.Col] as string;
        var okPressed = false;

        var ok = new Button("Ok", is_default: true);
        ok.Clicked += () => { okPressed = true; Application.RequestStop(); };
        var cancel = new Button("Cancel");
        cancel.Clicked += () => { Application.RequestStop(); };
        var dialog = new Dialog("Enter a value", ok, cancel);
        var label = new Label
        {
            X = 0,
            Y = 1,
            Text = e.Table.Rows[e.Row][0].ToString(),
        };
        var textField = new TextField
        {
            Text = oldValue ?? "",
            X = 0,
            Y = 2,
            Width = Dim.Fill(),
        };

        dialog.Add(label, textField);
        textField.SetFocus();
        Application.Run(dialog);

        if (okPressed)
        {
            var newValue = textField.Text.ToString();
            e.Table.Rows[e.Row][e.Col] = newValue as object ?? DBNull.Value;
            VariableTableView?.Update();
        }
    }

    private void RequestListKeyPressed(KeyEventEventArgs args)
    {
        if (args.KeyEvent.Key == Key.Enter)
        {
            _ = SendSelectedRequest();
        }
    }

    private void RequestListClick(MouseEventArgs args)
    {
        if (args.MouseEvent.Flags.HasFlag(MouseFlags.Button1DoubleClicked))
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
        var result = await _blooper.SendRequest(_selectedConfig, _selectedRequest);
        await result.MatchAsync(async response =>
        {
            var sb = new StringBuilder();
            sb.AppendLine($"{response.RequestMessage?.Method.ToString().ToUpper()} {response.RequestMessage?.RequestUri?.ToString()}");
            sb.AppendLine($"HTTP/{response.RequestMessage?.Version} {(int)response.StatusCode} {response.ReasonPhrase}");
            foreach (var (k, v) in response.Headers)
            {
                sb.AppendLine($"{k}: {v.Aggregate((a, b) => $"{a},{b}")}");
            }

            ResultDetails.Text = sb.ToString();

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
            ResultDetails.Text = "Something bad happened!";
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
