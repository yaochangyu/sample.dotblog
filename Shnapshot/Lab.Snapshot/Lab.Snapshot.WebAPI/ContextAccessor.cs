namespace Lab.Snapshot.WebAPI;

public record AuthContext
{
    public string TraceId { get; set; }

    public DateTimeOffset Now { get; set; }

    public string UserId { get; set; }
}

public interface IContextGetter<T> where T : class
{
    T? Get();
}

public interface IContextSetter<T> where T : class
{
    void Set(T value);
}

public class ContextAccessor<T> : IContextGetter<T>, IContextSetter<T> where T : class
{
    private static AsyncLocal<ContextHolder<T>> _context = new();

    public T? Get() => _context.Value?.Context;

    public void Set(T value) => _context.Value = new ContextHolder<T> { Context = value };
}

public class ContextHolder<T> where T : class
{
    public T Context { get; set; }
}