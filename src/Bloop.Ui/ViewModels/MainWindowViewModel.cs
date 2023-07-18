using Bloop.Core;
using ReactiveUI.Fody.Helpers;
using System.Collections.Generic;

namespace Bloop.Avalonia.Ui.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    public Config BloopConfig { get; set; } = new Config
    {
        Request = new Dictionary<string, Request>
        {
            { "Derp", new Request
                {
                    Uri = "https://derp.com",
                }
            },
            { "Bloop", new Request
                {
                    Uri = "http://bloop.com"
                }
            },
        }
    };

    [Reactive]
    public KeyValuePair<string, Request>? SelectedRequest { get ; set; }
}
