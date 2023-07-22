using Bloop.Core;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace Bloop.Avalonia.Ui.Models;

public class RequestResult : ReactiveObject
{
    [Reactive]
    public Request Request { get; set; }
    [Reactive]
    public HttpResponseMessage Response { get; set; }

    public RequestResult(Request request, HttpResponseMessage response)
    {
        Request = request;
        Response = response;
    }
}
