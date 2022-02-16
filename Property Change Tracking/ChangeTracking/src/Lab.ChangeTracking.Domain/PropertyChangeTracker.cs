using System.Reflection;

namespace Lab.ChangeTracking.Domain.Annotations;

public class PropertyChangeTracker
{
    private Dictionary<string, object> _changedProperties = new();
    private Dictionary<string, object> _originalValues = new();

    public void Initial()
    {
        var properties = this.GetType().GetProperties();
        foreach (var property in properties)
        {
            this._originalValues.Add(property.Name, property.GetValue(this));
        }
    }

    public Dictionary<string, object> GetChangedProperties()
    {
        return this._changedProperties;
    }

    public Dictionary<string, object> GetOriginalValues()
    {
        throw new NotImplementedException();
    }

    public void Track(string propertyName, object value)
    {
        var changes = this._changedProperties;
        if (changes.ContainsKey(propertyName) == false)
        {
            changes.Add(propertyName, value);
        }
        else
        {
            changes[propertyName] = value;
        }
    }
}