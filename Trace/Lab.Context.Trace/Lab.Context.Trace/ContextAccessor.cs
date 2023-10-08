namespace Lab.Context.Trace;

public class ContextAccessor<T> : IContextSetter<T>, IContextGetter<T>
    where T : class
{
    private T _value;

    public void Set(T value)
    {
        this._value = value;
    }

    public T? Get()
    {
        return this._value;
    }
}