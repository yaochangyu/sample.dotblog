namespace Lab.Context.Trace;

public interface IObjectContextGetter<T>
{
    T Get();
}