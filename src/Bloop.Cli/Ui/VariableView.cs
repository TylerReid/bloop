using Bloop.Core;
using System.Data;
using Terminal.Gui.App;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Bloop.Cli.Ui;

internal class VariableView : Runnable
{
    private readonly Config _config;
    private readonly DataTable _scratchVariables = new();
    private TableView VariableTableView { get; set; }
    private StatusBar VariableStatusBar { get; set; }

    public VariableView(Config config)
    {
        _config = config;

        X = 0;
        Y = 0;
        Width = Dim.Fill();
        Height = Dim.Fill();

        _scratchVariables.Columns.Add("Name", typeof(string));
        _scratchVariables.Columns.Add("Value", typeof(string));
        foreach (var variable in _config.Variables)
        {
            _scratchVariables.Rows.Add(variable.Name, variable.Value);
        }

        var frame = new FrameView
        {
            Title = "Variables",
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(1),
            CanFocus = true,
        };

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

        VariableStatusBar = new StatusBar
        {
            SchemeName = "Base",
        };
        VariableStatusBar.Add(
            new Shortcut(Key.Q.WithCtrl, "Back", SaveAndStop) { BindKeyToApplication = true }
        );

        Add(frame);
        Add(VariableStatusBar);
    }

    private void SaveAndStop()
    {
        foreach (DataRow row in _scratchVariables.Rows)
        {
            var variable = _config.Variables
                .First(x => x.Name == (string)row["Name"]);
            variable.Value = row["Value"] as string;
            variable.SatisfiedEnv = _config.Env;
        }
        App!.RequestStop();
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
            VariableTableView.SetNeedsDraw();
        }
    }
}
