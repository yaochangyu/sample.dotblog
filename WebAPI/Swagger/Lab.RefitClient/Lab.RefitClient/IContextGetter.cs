namespace Lab.RefitClient
{
    public interface IContextGetter<T> where T : class
    {
        T Get();
    }
}