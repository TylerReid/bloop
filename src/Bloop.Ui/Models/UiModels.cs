using Bloop.Core;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System.Collections.ObjectModel;

namespace Bloop.Avalonia.Ui.Models;

// todo reconsider if this is actually worthwhile
public class UiConfig : ReactiveObject
{
    [Reactive]
    public ObservableCollection<NamedObject<Request>> Requests { get; set; } = new();
    [Reactive]
    public ObservableCollection<NamedObject<Variable>> Variables { get; set; } = new();
    [Reactive]
    public Defaults? Defaults { get; set; }
}

public class RequestResult : ReactiveObject
{
    [Reactive]
    public NamedObject<Request> Request { get; set; }
    [Reactive]
    public HttpResponseMessage Response { get; set; }

    public RequestResult(NamedObject<Request> request, HttpResponseMessage response)
    {
        Request = request;
        Response = response;
    }
}

public class NamedObject<T> : ReactiveObject
{
    [Reactive]
    public string? Name { get; set; }
    [Reactive]
    public T? Value { get; set; }

    public override string ToString() => ModelHelper.ToString(this);
}
