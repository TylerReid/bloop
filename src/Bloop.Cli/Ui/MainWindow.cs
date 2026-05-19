using Bloop.Core;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Terminal.Gui.App;
using Terminal.Gui.Configuration;
using Terminal.Gui.Document;
using Terminal.Gui.Drawing;
using Terminal.Gui.Editor;
using Terminal.Gui.Highlighting;
using Terminal.Gui.Views;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Input;

namespace Bloop.Cli.Ui;

internal class MainWindow : Runnable
{
    private List<Config> _configs = new();
    private Config? _selectedConfig;
    private Request? _selectedRequest;
    private readonly Blooper _blooper = new();
    private readonly DataTable _scratchVariables = new();

    private FrameView LeftPane { get; set; }
    private ListView RequestListView { get; set; }
    private FrameView RightPane { get; set; }
    private MenuBar ResultsMenuBar { get; set; }
    private Editor ResultDetails { get; set; }
    private Editor ResultsView { get; set; }
    private StatusBar MainStatusBar { get; set; }
    private Shortcut ProcessingItem { get; set; }
    private Shortcut ThemeItem { get; set; }
    private StatusBar VariableStatusBar { get; set; }
    private StatusBar ThemeStatusBar { get; set; }
    private TableView? VariableTableView { get; set; }
    private ListView? VariableSetListView { get; set; }
    private ListView? ThemeListView { get; set; }
    private Shortcut SelectedVariableSet { get; set; }
    private MenuItem SaveFile { get; set; }
    private MenuItem CopyToClipboard { get; set; }
    private string _themeOriginalName = "";
    
    // #002663 amex blue
    // #5fbb70 kabbage green

    public MainWindow()
    {
        X = 0;
        Y = 0;
        Width = Dim.Fill();
        Height = Dim.Fill();

        LeftPane = new FrameView
        {
            Title = "Requests",
            X = 0,
            Y = 0,
            Width = Dim.Fill(Dim.Func(_ => RightPane!.Frame.Width)),
            Height = Dim.Fill(1),
            CanFocus = true,
        };
        RightPane = new FrameView
        {
            Title = "Results",
            X = Pos.Percent(25),
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(1),
            Arrangement = ViewArrangement.LeftResizable,
            SuperViewRendersLineCanvas = true,
            CanFocus = true,
        };

        RequestListView = new ListView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
        };

        RequestListView.ValueChanged += RequestSelectionChanged;
        RequestListView.KeyDown += RequestListKeyDown;
        RequestListView.MouseEvent += RequestListMouseEvent;

        LeftPane.Add(RequestListView);

        SaveFile = new MenuItem("Save Result", Key.Empty, SaveResultToFile);
        SaveFile.Enabled = false;
        CopyToClipboard = new MenuItem("Copy Result", Key.Empty, CopyResultToClipboard);
        CopyToClipboard.Enabled = false;
        
        ResultsMenuBar = new MenuBar
        ([
            new MenuBarItem("File", [
                SaveFile,
            ]),
            new MenuBarItem("Edit", [
                CopyToClipboard,
            ]),
        ]);

        var menuLine = new Line
        {
            X = 0,
            Y = Pos.Bottom(ResultsMenuBar),
            Width = Dim.Fill(),
            Orientation = Orientation.Horizontal,
        };

        ResultDetails = new Editor
        {
            X = 0,
            Y = Pos.Bottom(menuLine),
            Width = Dim.Fill(),
            Height = Dim.Percent(10),
            CanFocus = true,
            ReadOnly = true,
        };

        var line = new Line
        {
            X = 0,
            Y = Pos.Bottom(ResultDetails),
            Width = Dim.Fill(),
            Orientation = Orientation.Horizontal,
        };

        ResultsView = new Editor
        {
            X = 0,
            Y = Pos.Bottom(line),
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            CanFocus = true,
            ReadOnly = true,
            ViewportSettings = ViewportSettingsFlags.HasScrollBars,
            GutterOptions = GutterOptions.None,
        };

        RightPane.Add(ResultsMenuBar);
        RightPane.Add(menuLine);
        RightPane.Add(ResultDetails);
        RightPane.Add(line);
        RightPane.Add(ResultsView);

        ProcessingItem = new Shortcut(Key.Empty, "", null) { BindKeyToApplication = false };
        ThemeItem = new Shortcut(Key.T.WithCtrl, $"Theme: {ThemeManager.GetCurrentThemeName()}", SwitchToThemeView) { BindKeyToApplication = true };
        SelectedVariableSet = new Shortcut(Key.X.WithCtrl, "", SwitchVariableSet) { BindKeyToApplication = true };
        
        MainStatusBar = new StatusBar
        {
            SchemeName = "Base",
        };
        MainStatusBar.Add(
            new Shortcut(Key.Q.WithCtrl, "Quit", () => App!.RequestStop()) { BindKeyToApplication = true },
            new Shortcut(Key.V.WithAlt, "Variables", SwitchToVariableView) { BindKeyToApplication = true },
            new Shortcut(Key.Tab.WithCtrl, "Switch Bloops", CycleConfigs) { BindKeyToApplication = true },
            SelectedVariableSet,
            new Shortcut(Key.R.WithCtrl, "Reload", () => _ = LoadAsync()) { BindKeyToApplication = true },
            ThemeItem,
            ProcessingItem
        );

        VariableStatusBar = new StatusBar
        {
            SchemeName = "Base",
        };
        VariableStatusBar.Add(
            new Shortcut(Key.Q.WithCtrl, "Back", SwitchToMainView) { BindKeyToApplication = true }
        );

        ThemeStatusBar = new StatusBar
        {
            SchemeName = "Base",
        };
        ThemeStatusBar.Add(
            new Shortcut(Key.Enter, "Apply", ApplyThemeAndReturn) { BindKeyToApplication = true },
            new Shortcut(Key.Q.WithCtrl, "Cancel", CancelThemeAndReturn) { BindKeyToApplication = true }
        );

        _scratchVariables.Columns.Add("Name", typeof(string));
        _scratchVariables.Columns.Add("Value", typeof(string));

        RefreshSelectedEnvDisplay();
        SwitchToMainView();

        _ = LoadAsync();
    }

    private void SaveResultToFile()
    {
        if (ResultsView.Document?.Text == null)
        {
            return;
        }
        var dialog = new SaveDialog();
        dialog.Title = "Save Result";
        App!.Run(dialog);
        if (dialog.FileName != null)
        {
            File.WriteAllText(dialog.FileName, ResultsView.Document.Text);
        }
    }

    private void CopyResultToClipboard()
    {
        if (ResultsView.Document?.Text != null)
        {
            App!.Clipboard?.TrySetClipboardData(ResultsView.Document.Text);
        }
    }

    private void SwitchToMainView()
    {
        foreach (DataRow row in _scratchVariables.Rows)
        {
            var variable = _selectedConfig!.Variables
                .First(x => x.Name == (string)row["Name"]);
            variable.Value = row["Value"] as string;
            variable.SatisfiedEnv = _selectedConfig.Env;
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

        var frame = new FrameView
        {
            Title = "Variables",
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
            Table = new DataTableSource(_scratchVariables),
        };

        VariableTableView.Accepted += (_, _) =>
        {
            var cell = VariableTableView.Value!.SelectedCell;
            EditCurrentCell(cell.X, cell.Y);
        };

        frame.Add(VariableTableView);
        Add(frame);
        Add(VariableStatusBar);
    }

    private void SwitchVariableSet()
    {
        if (_selectedConfig == null) { return; }

        var allSets = _selectedConfig.Variables
            .Where(x => x.VariableSets is not null)
            .SelectMany(x => x.VariableSets!.Keys)
            .Distinct()
            .ToList();

        VariableSetListView = new ListView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(1),
        };
        VariableSetListView.SetSource(new ObservableCollection<string>(allSets));

        var okPressed = false;
        var shouldClear = false;

        VariableSetListView.MouseEvent += (_, e) =>
        {
            if (e.Flags.HasFlag(MouseFlags.LeftButtonDoubleClicked))
            {
                okPressed = true;
                App!.RequestStop();
                e.Handled = true;
            }
        };

        var ok = new Button { Text = "Ok", IsDefault = true };
        ok.Accepted += (_, _) => { okPressed = true; App!.RequestStop(); };
        var cancel = new Button { Text = "Cancel" };
        cancel.Accepted += (_, _) => { App!.RequestStop(); };
        var clear = new Button { Text = "Clear Env" };
        clear.Accepted += (_, _) => { shouldClear = true; App!.RequestStop(); };

        var dialog = new Dialog { Title = "Select Variable Set" };
        dialog.AddButton(ok);
        dialog.AddButton(cancel);
        dialog.AddButton(clear);
        dialog.Add(VariableSetListView);
        VariableSetListView.HasFocus = true;

        App!.Run(dialog);
        dialog.Dispose();

        if (shouldClear)
        {
            _selectedConfig.Env = null;
        }

        if (okPressed && VariableSetListView.SelectedItem.HasValue)
        {
            _selectedConfig.Env = allSets[VariableSetListView.SelectedItem.Value];
        }
        RefreshSelectedEnvDisplay();
    }

    private void RefreshSelectedEnvDisplay()
    {
        SelectedVariableSet.Title = $"Set: {_selectedConfig?.Env ?? "None"}";
        MainStatusBar.SetNeedsDraw();
    }

    private void EditCurrentCell(int col, int row)
    {
        if (col != 1) { return; }
        var oldValue = _scratchVariables.Rows[row][col] as string;

        var ok = new Button { Text = "Ok", IsDefault = true };
        var cancel = new Button { Text = "Cancel" };

        var dialog = new Dialog { Title = "Enter a value" };
        dialog.Width = Dim.Auto(minimumContentDim: Dim.Percent(50));
        dialog.AddButton(cancel);
        dialog.AddButton(ok);

        var label = new Label
        {
            X = 0,
            Y = 1,
            Text = _scratchVariables.Rows[row][0]?.ToString() ?? string.Empty,
        };
        var textField = new TextField
        {
            Text = oldValue ?? "",
            X = 0,
            Y = 2,
            Width = Dim.Fill(),
        };

        dialog.Add(label, textField);
        textField.HasFocus = true;
        App!.Run(dialog);
        dialog.Dispose();

        if (dialog.Result == 1)
        {
            var newValue = textField.Text;
            _scratchVariables.Rows[row][col] = newValue as object ?? DBNull.Value;
            VariableTableView?.SetNeedsDraw();
        }
    }

    private void RequestListKeyDown(object? sender, Key args)
    {
        if (args == Key.Enter)
        {
            _ = SendSelectedRequest();
            args.Handled = true;
        }
    }

    private void RequestListMouseEvent(object? sender, Mouse args)
    {
        if (args.Flags.HasFlag(MouseFlags.LeftButtonDoubleClicked))
        {
            _ = SendSelectedRequest();
            args.Handled = true;
        }
    }

    private async Task SendSelectedRequest()
    {
        if (_selectedConfig == null || _selectedRequest == null) { return; }
        
        ProcessingItem.Title = "Sending bloop";
        ResultsView.Document = new TextDocument("");

        var stopwatch = new Stopwatch();
        stopwatch.Start();
        var result = await _blooper.SendRequest(_selectedConfig, _selectedRequest);
        
        string detailsText = "";
        string responseText = "";
        bool isJson = false;
        bool isXml = false;
        bool hasError = false;

        await result.MatchAsync(async response =>
        {
            var sb = new StringBuilder();
            sb.AppendLine($"{response.RequestMessage?.Method.ToString().ToUpper()} {response.RequestMessage?.RequestUri?.ToString()}");
            sb.AppendLine($"HTTP/{response.RequestMessage?.Version} {(int)response.StatusCode} {response.ReasonPhrase}");
            foreach (var (k, v) in response.Headers)
            {
                sb.AppendLine($"{k}: {v.Aggregate((a, b) => $"{a},{b}")}");
            }
            detailsText = sb.ToString();

            var mediaType = response.Content.Headers.ContentType?.MediaType ?? "";
            if (mediaType.Contains("json"))
            {
                isJson = true;
                var parsedJson = JsonNode.Parse(await response.Content!.ReadAsStreamAsync());
                responseText = parsedJson!.ToJsonString(options: new JsonSerializerOptions { WriteIndented = true });
            }
            else if (mediaType.Contains("xml"))
            {
                isXml = true;
                responseText = await response.Content.ReadAsStringAsync();
            }
            else
            {
                responseText = await response.Content.ReadAsStringAsync();
            }
        },
        error =>
        {
            hasError = true;
            detailsText = "Something bad happened!";
            responseText = error.Message;
            return Task.CompletedTask;
        });

        stopwatch.Stop();
        var elapsed = stopwatch.Elapsed;
        
        App!.Invoke(() =>
        {
            ResultDetails.Document = new TextDocument(detailsText);
            ResultsView.GutterOptions = GutterOptions.LineNumbers | GutterOptions.Folding;
            if (hasError || (!isJson && !isXml))
            {
                ResultsView.HighlightingDefinition = null;
                ResultsView.Document = new TextDocument(responseText);
            }
            else if (isJson)
            {
                ResultsView.HighlightingDefinition = HighlightingManager.Instance.GetDefinitionByExtension(".json");
                ResultsView.Document = new TextDocument(responseText);
            }
            else
            {
                ResultsView.HighlightingDefinition = HighlightingManager.Instance.GetDefinitionByExtension(".xml");
                ResultsView.Document = new TextDocument(responseText);
            }
            SaveFile.Enabled = !string.IsNullOrWhiteSpace(ResultsView.Document.Text);
            CopyToClipboard.Enabled = !string.IsNullOrWhiteSpace(ResultsView.Document.Text);
            ProcessingItem.Title = $"Response Time: {elapsed}";
        });
    }

    private void RequestSelectionChanged(object? sender, ValueChangedEventArgs<int?> args)
    {
        if (args.NewValue.HasValue)
        {
            _selectedRequest = _selectedConfig!.Requests[args.NewValue.Value];
        }
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
        RequestListView.SetSource(new ObservableCollection<string>(
            config.Requests.Select(x => x.Name)));
    }

    private void CycleConfigs()
    {
        if (_selectedConfig == null)
        {
            SelectConfig(_configs.FirstOrDefault());
            return;
        }
        var index = _configs.IndexOf(_selectedConfig) + 1;
        var newConfig = _configs[index >= _configs.Count ? 0 : index];
        SelectConfig(newConfig);
    }

    private void SwitchToThemeView()
    {
        _themeOriginalName = ThemeManager.GetCurrentThemeName();
        var themeNames = ThemeManager.GetThemeNames().ToList();
        var currentIndex = Math.Max(0, themeNames.IndexOf(_themeOriginalName));

        RemoveAll();

        var frame = new FrameView
        {
            Title = "Select Theme",
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(1),
            CanFocus = true,
        };

        ThemeListView = new ListView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
        };
        ThemeListView.SetSource(new ObservableCollection<string>(themeNames));
        ThemeListView.SetSelection(currentIndex, false);

        ThemeListView.ValueChanged += (_, e) =>
        {
            if (e.NewValue.HasValue && e.NewValue.Value < themeNames.Count)
            {
                ThemeManager.Theme = themeNames[e.NewValue.Value];
                ConfigurationManager.Apply();
                SetNeedsDraw();
            }
        };

        frame.Add(ThemeListView);
        Add(frame);
        Add(ThemeStatusBar);
        ThemeListView.HasFocus = true;
    }

    private void ApplyThemeAndReturn()
    {
        if (ThemeListView?.SelectedItem.HasValue == true)
        {
            var themeNames = ThemeManager.GetThemeNames().ToList();
            if (ThemeListView.SelectedItem.Value < themeNames.Count)
            {
                SaveThemePreference(themeNames[ThemeListView.SelectedItem.Value]);
            }
        }
        ThemeListView = null;
        SwitchToMainView();
        RefreshThemeDisplay();
    }

    private void CancelThemeAndReturn()
    {
        ThemeManager.Theme = _themeOriginalName;
        ConfigurationManager.Apply();
        ThemeListView = null;
        SwitchToMainView();
        RefreshThemeDisplay();
    }

    private static void SaveThemePreference(string themeName)
    {
        var tuiDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".tui");
        Directory.CreateDirectory(tuiDir);
        var configPath = Path.Combine(tuiDir, "bloop.config.json");
        File.WriteAllText(configPath, JsonSerializer.Serialize(
            new { Theme = themeName },
            new JsonSerializerOptions { WriteIndented = true }));
    }

    private void RefreshThemeDisplay()
    {
        ThemeItem.Title = $"Theme: {ThemeManager.GetCurrentThemeName()}";
        MainStatusBar.SetNeedsDraw();
    }
}
