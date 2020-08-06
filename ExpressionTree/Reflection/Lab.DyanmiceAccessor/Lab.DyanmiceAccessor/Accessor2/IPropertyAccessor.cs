namespace Lab.DynamicAccessor.Accessor2
{
    public interface IPropertyAccessor
    {
        object GetValue(object instance);

        void SetValue(object instance, object value);
    }
}