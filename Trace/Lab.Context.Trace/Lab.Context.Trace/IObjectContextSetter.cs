namespace Lab.Context.Trace;

public interface IObjectContextSetter<T>
{
    void Set(T value);
}