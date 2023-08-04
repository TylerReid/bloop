using System.Collections.ObjectModel;
using Bloop.Core;

namespace Bloop.Ui.ViewModels;

public class DesignMainViewModel : MainWindowViewModel
{
    public DesignMainViewModel()
    {
        Configs = new ObservableCollection<Config>
        {
            new()
            {
                Directory = "/path",
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