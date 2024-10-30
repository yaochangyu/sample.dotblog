namespace Lab.Sharding.Infrastructure.TraceContext;

public interface IContextGetter<T>
{
    T? Get();
}