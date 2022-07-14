namespace Bloop.Core;

public abstract class Either<T1, T2>
{
    public abstract TResult Match<TResult>(Func<T1, TResult> f1, Func<T2, TResult> f2);
    public abstract void Match(Action<T1> a1, Action<T2> a2);

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

        public override TResult Match<TResult>(Func<T1, TResult> f1, Func<T2, TResult> f2)
        {
            return f1(Item);
        }

        public override void Match(Action<T1> a1, Action<T2> a2)
        {
            a1(Item);
        }
    }

    private sealed class Case2 : Either<T1, T2>
    {
        public T2 Item { get; }

        public Case2(T2 item)
        {
            Item = item;
        }

        public override TResult Match<TResult>(Func<T1, TResult> f1, Func<T2, TResult> f2)
        {
            return f2(Item);
        }

        public override void Match(Action<T1> a1, Action<T2> a2)
        {
            a2(Item);
        }
    }
}