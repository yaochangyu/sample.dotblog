namespace Lab.Context.Trace;

public interface IContextGetter<T>
{
    T? Get();
}