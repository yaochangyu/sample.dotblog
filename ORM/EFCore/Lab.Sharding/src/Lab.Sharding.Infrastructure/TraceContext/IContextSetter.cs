namespace Lab.Sharding.Infrastructure.TraceContext;

public interface IContextSetter<T>
{
    void Set(T value);
}