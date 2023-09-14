namespace Lab.RefitClient
{
    public interface IContextSetter<T> where T : class
    {
        void Set(T trackContext);
    }
}