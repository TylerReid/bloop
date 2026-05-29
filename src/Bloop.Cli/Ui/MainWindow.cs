using Bloop.Core;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net.Http.Handlers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Terminal.Gui.App;
using Terminal.Gui.Configuration;
using Terminal.Gui.Document;
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
    private readonly Blooper _blooper;

    private FrameView LeftPane { get; set; }
    private ListView RequestListView { get; set; }
    private FrameView RightPane { get; set; }
    private MenuBar ResultsMenuBar { get; set; }
    private Editor ResultDetails { get; set; }
    private View DetailsView { get; set; }
    private Editor ResultsView { get; set; }
    private StatusBar MainStatusBar { get; set; }
    private Shortcut ProcessingItem { get; set; }
    private Shortcut ThemeItem { get; set; }
    private ListView? VariableSetListView { get; set; }
    private Shortcut SelectedVariableSet { get; set; }
    private MenuItem SaveFile { get; set; }
    private MenuItem CopyToClipboard { get; set; }
    private ProgressBar RequestSpinner { get; set; }
    
    // #002663 amex blue
    // #5fbb70 kabbage green

    public MainWindow()
    {
        var httpHandler = new ProgressMessageHandler
        {
            InnerHandler = new HttpClientHandler(),
        };
        httpHandler.HttpReceiveProgress += (_, _) => PulseSpinner();
        httpHandler.HttpSendProgress += (_, _) => PulseSpinner();
        _blooper =  new Blooper(new HttpClient(httpHandler));
        
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
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            CanFocus = true,
            ReadOnly = true,
        };
        
        DetailsView = new View()
        {
            X = 0,
            Y = Pos.Bottom(menuLine),
            Width = Dim.Fill(),
            Height = Dim.Percent(10),
        };
        
        DetailsView.Add(ResultDetails);

        var line = new Line
        {
            X = 0,
            Y = Pos.Bottom(DetailsView),
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
        RightPane.Add(DetailsView);
        RightPane.Add(line);
        RightPane.Add(ResultsView);

        ProcessingItem = new Shortcut(Key.Empty, "", null) { BindKeyToApplication = false };
        ThemeItem = new Shortcut(Key.T.WithCtrl, $"Theme", SwitchToThemeView) { BindKeyToApplication = true };
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
            new Shortcut(Key.E.WithCtrl, "ScratchPad", SwitchToScratchPadView) { BindKeyToApplication = true },
            ProcessingItem
        );

        CreateRequestSpinner();
        ActivityPulsar.ActivityStarted += (_, _) => PulseSpinner();

        RefreshSelectedEnvDisplay();
        Add(LeftPane);
        Add(RightPane);
        Add(MainStatusBar);

        _ = LoadAsync();
    }

    private void CreateRequestSpinner()
    {
        RequestSpinner = new ProgressBar
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            ProgressBarStyle = ProgressBarStyle.MarqueeContinuous,
            BidirectionalMarquee = false,
            SyncWithTerminal = false,
        };
    }

    private void PulseSpinner() => App!.Invoke(_ => RequestSpinner.Pulse());

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

    private void SwitchToVariableView()
    {
        if (_selectedConfig == null) { return; }
        var variableView = new VariableView(_selectedConfig);
        App!.Run(variableView);
        variableView.Dispose();
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
        
        App!.Invoke(_ =>
        {
            ProcessingItem.Title = "Sending bloop";
            DetailsView.RemoveAll();
            DetailsView.Add(RequestSpinner);
            ResultsView.Document = new TextDocument("");
        });

        var stopwatch = new Stopwatch();
        stopwatch.Start();
        var result = await _blooper.SendRequest(_selectedConfig, _selectedRequest);
        
        var detailsText = "";
        var responseText = "";
        var isJson = false;
        var isXml = false;
        var hasError = false;

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
            DetailsView.RemoveAll();
            CreateRequestSpinner();
            DetailsView.Add(ResultDetails);
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
        using var themeView = new ThemeView();
        App!.Run(themeView);
        ThemeItem.Title = $"Theme: {ThemeManager.GetCurrentThemeName()}";
        MainStatusBar.SetNeedsDraw();
    }

    private void SwitchToScratchPadView()
    {
        if (_selectedConfig == null || _selectedRequest == null)
        {
            return;
        }
        
        using var scratchPad = new ScratchPadView(_blooper, _selectedConfig, _selectedRequest);
        App!.Run(scratchPad);
    }
}
