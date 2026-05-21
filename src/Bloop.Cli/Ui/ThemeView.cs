using System.Collections.ObjectModel;
using System.Text.Json;
using Terminal.Gui.Configuration;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Bloop.Cli.Ui;

internal class ThemeView : Runnable
{
    private readonly string _originalThemeName;
    private readonly List<string> _themeNames;
    private ListView ThemeListView { get; set; }

    public ThemeView()
    {
        _originalThemeName = ThemeManager.GetCurrentThemeName();
        _themeNames = ThemeManager.GetThemeNames().ToList();
        var currentIndex = Math.Max(0, _themeNames.IndexOf(_originalThemeName));

        X = 0;
        Y = 0;
        Width = Dim.Fill();
        Height = Dim.Fill();

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
        ThemeListView.SetSource(new ObservableCollection<string>(_themeNames));
        ThemeListView.SetSelection(currentIndex, false);

        ThemeListView.ValueChanged += (_, e) =>
        {
            if (e.NewValue.HasValue && e.NewValue.Value < _themeNames.Count)
            {
                ThemeManager.Theme = _themeNames[e.NewValue.Value];
                ConfigurationManager.Apply();
                SetNeedsDraw();
            }
        };

        frame.Add(ThemeListView);

        var statusBar = new StatusBar { SchemeName = "Base" };
        statusBar.Add(
            new Shortcut(Key.Enter, "Apply", Apply) { BindKeyToApplication = true },
            new Shortcut(Key.Q.WithCtrl, "Cancel", Cancel) { BindKeyToApplication = true }
        );

        Add(frame);
        Add(statusBar);
        ThemeListView.HasFocus = true;
    }

    private void Apply()
    {
        if (ThemeListView.SelectedItem.HasValue && ThemeListView.SelectedItem.Value < _themeNames.Count)
        {
            SaveThemePreference(_themeNames[ThemeListView.SelectedItem.Value]);
        }
        App!.RequestStop();
    }

    private void Cancel()
    {
        ThemeManager.Theme = _originalThemeName;
        ConfigurationManager.Apply();
        App!.RequestStop();
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
}
