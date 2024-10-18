namespace JobBank1111.Infrastructure.TraceContext;

public interface IContextGetter<T>
{
    T? Get();
}