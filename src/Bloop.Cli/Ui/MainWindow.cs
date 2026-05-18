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
    private TextView ResultDetails { get; set; }
    private TextView ResultsView { get; set; }
    private StatusBar MainStatusBar { get; set; }
    private Shortcut ProcessingItem { get; set; }
    private Shortcut ThemeItem { get; set; }
    private StatusBar VariableStatusBar { get; set; }
    private TableView? VariableTableView { get; set; }
    private ListView? VariableSetListView { get; set; }
    private Shortcut SelectedVariableSet { get; set; }

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
            Width = Dim.Percent(25),
            Height = Dim.Fill(1),
            CanFocus = true,
        };
        RightPane = new FrameView
        {
            Title = "Results",
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
        };

        RequestListView.ValueChanged += RequestSelectionChanged;
        RequestListView.KeyDown += RequestListKeyDown;
        RequestListView.MouseEvent += RequestListMouseEvent;

        LeftPane.Add(RequestListView);

        ResultDetails = new TextView
        {
            X = 0,
            Y = 0,
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

        ResultsView = new TextView
        {
            X = 0,
            Y = Pos.Bottom(line),
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            CanFocus = true,
            ReadOnly = true,
        };

        RightPane.Add(ResultDetails);
        RightPane.Add(line);
        RightPane.Add(ResultsView);

        ProcessingItem = new Shortcut(Key.Empty, "", null) { BindKeyToApplication = false };
        ThemeItem = new Shortcut(Key.T.WithCtrl, $"Theme: {ThemeManager.GetCurrentThemeName()}", PickTheme) { BindKeyToApplication = true };
        SelectedVariableSet = new Shortcut(Key.X.WithCtrl, "", SwitchVariableSet) { BindKeyToApplication = true };

        MainStatusBar = new StatusBar();
        MainStatusBar.Add(
            new Shortcut(Key.Q.WithCtrl, "Quit", () => App!.RequestStop()) { BindKeyToApplication = true },
            new Shortcut(Key.V.WithAlt, "Variables", SwitchToVariableView) { BindKeyToApplication = true },
            new Shortcut(Key.C.WithCtrl, "Copy Result", CopyResultToClipboard) { BindKeyToApplication = true },
            new Shortcut(Key.Tab.WithCtrl, "Switch Bloops", CycleConfigs) { BindKeyToApplication = true },
            SelectedVariableSet,
            ThemeItem,
            ProcessingItem
        );

        VariableStatusBar = new StatusBar();
        VariableStatusBar.Add(
            new Shortcut(Key.Q.WithCtrl, "Back", SwitchToMainView) { BindKeyToApplication = true },
            new Shortcut(Key.T.WithCtrl, "Theme", PickTheme) { BindKeyToApplication = true }
        );

        _scratchVariables.Columns.Add("Name", typeof(string));
        _scratchVariables.Columns.Add("Value", typeof(string));

        RefreshSelectedEnvDisplay();
        SwitchToMainView();

        _ = LoadAsync();
    }

    private void CopyResultToClipboard()
    {
        if (ResultsView.Text != null)
        {
            App!.Clipboard?.TrySetClipboardData(ResultsView.Text);
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
        var okPressed = false;

        var ok = new Button { Text = "Ok", IsDefault = true };
        ok.Accepted += (_, _) => { okPressed = true; App!.RequestStop(); };
        var cancel = new Button { Text = "Cancel" };
        cancel.Accepted += (_, _) => { App!.RequestStop(); };

        var dialog = new Dialog { Title = "Enter a value" };
        dialog.AddButton(ok);
        dialog.AddButton(cancel);

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

        if (okPressed)
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

    private void PickTheme()
    {
        var themeNames = ThemeManager.GetThemeNames().ToList();
        var originalTheme = ThemeManager.GetCurrentThemeName();
        var currentIndex = Math.Max(0, themeNames.IndexOf(originalTheme));

        var themeListView = new ListView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(1),
        };
        themeListView.SetSource(new ObservableCollection<string>(themeNames));
        themeListView.SetSelection(currentIndex, false);

        themeListView.ValueChanged += (_, e) =>
        {
            if (e.NewValue.HasValue && e.NewValue.Value < themeNames.Count)
            {
                ThemeManager.Theme = themeNames[e.NewValue.Value];
                ConfigurationManager.Apply();
            }
        };

        var confirmed = false;

        var ok = new Button { Text = "Apply", IsDefault = true };
        ok.Accepted += (_, _) => { confirmed = true; App!.RequestStop(); };
        var cancel = new Button { Text = "Cancel" };
        cancel.Accepted += (_, _) => { App!.RequestStop(); };

        var dialog = new Dialog { Title = "Select Theme" };
        dialog.AddButton(ok);
        dialog.AddButton(cancel);
        dialog.Add(themeListView);
        themeListView.HasFocus = true;

        App!.Run(dialog);
        dialog.Dispose();

        if (confirmed && themeListView.SelectedItem.HasValue)
        {
            var selected = themeNames[themeListView.SelectedItem.Value];
            SaveThemePreference(selected);
        }
        else
        {
            ThemeManager.Theme = originalTheme;
            ConfigurationManager.Apply();
        }

        RefreshThemeDisplay();
        SetNeedsDraw();
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
