namespace Lab.DynamicAccessor
{
    public interface IConstructorAccessor
    {
        object Execute(params object[] parameters);
    }
}