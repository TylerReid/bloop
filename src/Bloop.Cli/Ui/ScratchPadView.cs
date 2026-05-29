using Bloop.Core;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Terminal.Gui.Document;
using Terminal.Gui.Editor;
using Terminal.Gui.Highlighting;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Bloop.Cli.Ui;

public class ScratchPadView : Runnable
{
    private readonly Blooper _blooper;
    private readonly Config _config;
    private readonly Editor _editor;
    private readonly Editor _resultsView;
    private readonly FrameView _resultPane;
    private readonly StatusBar _statusBar;
    private readonly Shortcut _statusItem;

    public ScratchPadView(Blooper blooper, Config config, Request initialRequest)
    {
        _blooper = blooper;
        _config = config;

        X = 0;
        Y = 0;
        Width = Dim.Fill();
        Height = Dim.Fill();

        _resultPane = new FrameView
        {
            Title = "Result",
            X = 0,
            Y = Pos.Percent(50),
            Width = Dim.Fill(),
            Height = Dim.Fill(1),
            Arrangement = ViewArrangement.TopResizable,
            SuperViewRendersLineCanvas = true,
            CanFocus = true,
        };

        var requestPane = new FrameView
        {
            Title = "Request",
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(Dim.Func(_ => _resultPane.Frame.Height + 1)),
            CanFocus = true,
        };

        _editor = new Editor
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            CanFocus = true,
            ReadOnly = false,
            ViewportSettings = ViewportSettingsFlags.HasScrollBars,
            GutterOptions = GutterOptions.LineNumbers,
        };

        _editor.Document = new TextDocument(initialRequest.ToHttpString());
        requestPane.Add(_editor);

        _resultsView = new Editor
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            CanFocus = true,
            ReadOnly = true,
            ViewportSettings = ViewportSettingsFlags.HasScrollBars,
            GutterOptions = GutterOptions.None,
        };

        _resultPane.Add(_resultsView);

        _statusItem = new Shortcut(Key.Empty, "", null) { BindKeyToApplication = false };

        _statusBar = new StatusBar
        {
            SchemeName = "Base",
        };
        _statusBar.Add(
            new Shortcut(Key.Q.WithCtrl, "Back", () => App!.RequestStop()) { BindKeyToApplication = true },
            new Shortcut(Key.R.WithCtrl, "Send", () => _ = SendRequestAsync()) { BindKeyToApplication = true },
            _statusItem
        );

        Add(requestPane);
        Add(_resultPane);
        Add(_statusBar);
    }

    private async Task SendRequestAsync()
    {
        var httpText = _editor.Document?.Text ?? "";
        var request = Request.FromHttpString(httpText);

        App!.Invoke(_ =>
        {
            _statusItem.Title = "Sending...";
            _statusBar.SetNeedsDraw();
            _resultsView.Document = new TextDocument("");
        });

        var result = await _blooper.SendRequest(_config, request);

        var responseText = "";
        var isJson = false;
        var isXml = false;

        await result.MatchAsync(async response =>
        {
            var sb = new StringBuilder();
            sb.AppendLine($"{response.RequestMessage?.Method.ToString().ToUpper()} {response.RequestMessage?.RequestUri}");
            sb.AppendLine($"HTTP/{response.RequestMessage?.Version} {(int)response.StatusCode} {response.ReasonPhrase}");
            foreach (var (k, v) in response.Headers)
            {
                sb.AppendLine($"{k}: {v.Aggregate((a, b) => $"{a},{b}")}");
            }
            sb.AppendLine();

            var mediaType = response.Content.Headers.ContentType?.MediaType ?? "";
            if (mediaType.Contains("json"))
            {
                isJson = true;
                var parsed = JsonNode.Parse(await response.Content.ReadAsStreamAsync());
                sb.Append(parsed!.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
            }
            else
            {
                isXml = mediaType.Contains("xml");
                sb.Append(await response.Content.ReadAsStringAsync());
            }

            responseText = sb.ToString();
        },
        error =>
        {
            responseText = $"Error: {error.Message}";
            return Task.CompletedTask;
        });

        App!.Invoke(() =>
        {
            if (isJson)
                _resultsView.HighlightingDefinition = HighlightingManager.Instance.GetDefinitionByExtension(".json");
            else if (isXml)
                _resultsView.HighlightingDefinition = HighlightingManager.Instance.GetDefinitionByExtension(".xml");
            else
                _resultsView.HighlightingDefinition = null;

            _resultsView.Document = new TextDocument(responseText);
            _resultsView.GutterOptions = GutterOptions.LineNumbers | GutterOptions.Folding;
            _statusItem.Title = "";
            _statusBar.SetNeedsDraw();
        });
    }
}