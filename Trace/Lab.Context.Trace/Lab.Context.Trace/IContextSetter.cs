namespace Lab.Context.Trace;

public interface IContextSetter<T>
{
    void Set(T value);
}