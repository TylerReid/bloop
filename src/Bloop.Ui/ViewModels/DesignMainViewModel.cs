using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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
                Request = new Dictionary<string, Request>
                {
                    {
                        "Derp", new Request
                        {
                            Uri = "https://derp.com",
                        }
                    },
                    {
                        "Bloop", new Request
                        {
                            Uri = "http://bloop.com"
                        }
                    },
                }
            }
        };

        BloopConfig = Configs.FirstOrDefault();
    }
}