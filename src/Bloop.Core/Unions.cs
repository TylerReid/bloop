namespace Bloop.Core;

public abstract class Either<T1, T2>
{
    public abstract TResult Match<TResult>(Func<T1, TResult> f1, Func<T2, TResult> f2);
    public abstract void Match(Action<T1> a1, Action<T2> a2);

    public abstract Task<TResult> MatchAsync<TResult>(Func<T1, Task<TResult>> f1, Func<T2, Task<TResult>> f2);
    public abstract Task MatchAsync(Func<T1, Task> f1, Func<T2, Task> f2);

    public abstract T1 UnwrapSuccess();
    public abstract T2 UnwrapFailure();

    private Either() { }

    public static implicit operator Either<T1, T2>(T1 item)
    {
        return new Case1(item);
    }

    public static implicit operator Either<T1, T2>(T2 item)
    {
        return new Case2(item);
    }

    public object? Unwrap() => Match<object?>(a => a, b => b);

    private sealed class Case1 : Either<T1, T2>
    {
        public T1 Item { get; }

        public Case1(T1 item)
        {
            Item = item;
        }

        public override TResult Match<TResult>(Func<T1, TResult> f1, Func<T2, TResult> f2) => f1(Item);
        public override void Match(Action<T1> a1, Action<T2> a2) => a1(Item);
        public override Task<TResult> MatchAsync<TResult>(Func<T1, Task<TResult>> f1, Func<T2, Task<TResult>> f2) => f1(Item);
        public override Task MatchAsync(Func<T1, Task> f1, Func<T2, Task> f2) => f1(Item);
        public override T1 UnwrapSuccess() => Item;
        public override T2 UnwrapFailure() => throw new Exception($"Bad {nameof(UnwrapFailure)}, was expecting {typeof(T1).Name} but found {typeof(T2).Name}");
    }

    private sealed class Case2 : Either<T1, T2>
    {
        public T2 Item { get; }

        public Case2(T2 item)
        {
            Item = item;
        }

        public override TResult Match<TResult>(Func<T1, TResult> f1, Func<T2, TResult> f2) => f2(Item);
        public override void Match(Action<T1> a1, Action<T2> a2) => a2(Item);
        public override Task<TResult> MatchAsync<TResult>(Func<T1, Task<TResult>> f1, Func<T2, Task<TResult>> f2) => f2(Item);
        public override Task MatchAsync(Func<T1, Task> f1, Func<T2, Task> f2) => f2(Item);
        public override T1 UnwrapSuccess() => throw new Exception($"Bad {nameof(UnwrapFailure)}, was expecting {typeof(T2).Name} but found {typeof(T1).Name}");
        public override T2 UnwrapFailure() => Item;
    }
}

public class Unit
{
    public static Unit Instance { get; } = new();
    private Unit() {}
}
