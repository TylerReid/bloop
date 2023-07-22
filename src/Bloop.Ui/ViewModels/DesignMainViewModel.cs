using System.Collections.ObjectModel;
using Bloop.Avalonia.Ui.Models;
using Bloop.Core;

namespace Bloop.Avalonia.Ui.ViewModels;

public class DesignMainViewModel : MainWindowViewModel
{
    public DesignMainViewModel()
    {
        Configs = new ObservableCollection<UiConfig>
        {
            new()
            {
                Requests = new ObservableCollection<NamedObject<Request>>
                {
                    new()
                    {
                        Name = "Derp",
                        Value = new()
                        {
                            Uri = "https://derp.com",
                        },
                    },
                    new()
                    {
                        Name = "Bloop",
                        Value = new()
                        {
                            Uri = "http://bloop.com",
                        },
                    },
                },
            },
        };

        BloopConfig = Configs.FirstOrDefault();
    }
}