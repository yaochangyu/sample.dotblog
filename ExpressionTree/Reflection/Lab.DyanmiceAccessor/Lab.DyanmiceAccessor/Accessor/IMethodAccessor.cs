namespace Lab.DynamicAccessor
{
    public interface IMethodAccessor
    {
        object Execute(object instance, params object[] parameters);
    }
}