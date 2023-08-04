using Bloop.Core;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System.Collections.ObjectModel;

namespace Bloop.Avalonia.Ui.Models;

public class RequestResult : ReactiveObject
{
    [Reactive]
    public Request Request { get; set; }
    [Reactive]
    public string Status { get; set; }
    [Reactive]
    public Error? Error { get; set; }
    [Reactive]
    public ObservableCollection<string> Headers { get; set; } = new ObservableCollection<string>();

    public RequestResult(Request request, Error error)
    {
        Request = request;
        Status = "Bloop Error";
        Error = error;
    }

    public RequestResult(Request request, HttpResponseMessage response)
    {
        Request = request;
        Status = response.StatusCode.ToString();
        var headerStrings = response.Headers
            .Select(x => $"{x.Key}: {x.Value.Aggregate((a, b) => $"{a} {b}")}")
            .ToList();
        Headers = new ObservableCollection<string>(headerStrings);
    }
}
