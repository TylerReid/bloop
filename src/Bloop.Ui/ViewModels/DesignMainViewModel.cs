using System.Collections.ObjectModel;
using Bloop.Core;

namespace Bloop.Avalonia.Ui.ViewModels;

public class DesignMainViewModel : MainWindowViewModel
{
    public DesignMainViewModel()
    {
        Configs = new ObservableCollection<Config>
        {
            new()
            {
                Requests = new List<Request>
                {
                    new()
                    {
                        Name = "Derp",
                        Uri = "https://derp.com",
                    },
                    new()
                    {
                        Name = "Bloop",
                        Uri = "http://bloop.com",
                    },
                },
            },
        };

        BloopConfig = Configs.FirstOrDefault();
    }
}