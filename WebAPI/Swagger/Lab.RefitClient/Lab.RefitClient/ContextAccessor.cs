namespace Lab.RefitClient
{
    public class ContextAccessor<T> : IContextSetter<T>, IContextGetter<T> where T : class
    {
        private static readonly AsyncLocal<T> s_current = new AsyncLocal<T>();

        public T Get()
        {
            return s_current?.Value;
        }

        public void Set(T value)
        {
            if (s_current == null)
            {
                return;
            }

            s_current.Value = value;
        }
    }
}