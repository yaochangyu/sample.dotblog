using System.ComponentModel;
using System.Reflection;

namespace Lab.ChangeTracking.Domain.Annotations;

public class Base : IRevertibleChangeTracking
{
    protected readonly Dictionary<string, object> ChangedProperties = new();
    protected readonly Dictionary<string, object> OriginalValues = new();

    public void Initialize()
    {
        var properties = this.GetType().GetProperties();

        // Save the current value of the properties to our dictionary.
        foreach (var property in properties)
        {
            this.OriginalValues.Add(property.Name, property.GetValue(this));
        }
    }

    public bool IsChanged { get; private set; }

    public void RejectChanges()
    {
        foreach (var property in this.ChangedProperties)
        {
            this.GetType().GetRuntimeProperty(property.Key).SetValue(this, property.Value);
        }

        this.AcceptChanges();
    }

    public void AcceptChanges()
    {
        this.ChangedProperties.Clear();
        this.IsChanged = false;
    }
}