namespace Lab.DynamicAccessor.Accessor2
{
    public interface IMethodAccessor
    {
        object Execute(object instance, params object[] parameters);
    }
}