namespace Lab.Context.Trace;

public class ContextAccessor2<T> : IContextSetter<T>, IContextGetter<T>
    where T : class
{
    private static readonly AsyncLocal<ContextHolder<T>> s_current = new();

    public T? Get()
    {
        var contextHolder = s_current.Value;
        return contextHolder?.Value;
    }

    public void Set(T value)
    {
        if (s_current.Value == null)
        {
            s_current.Value = new ContextHolder<T>();
        }

        s_current.Value.Value = value;
    }
}