namespace JobBank1111.Infrastructure.TraceContext;

public interface IContextSetter<T>
{
    void Set(T value);
}