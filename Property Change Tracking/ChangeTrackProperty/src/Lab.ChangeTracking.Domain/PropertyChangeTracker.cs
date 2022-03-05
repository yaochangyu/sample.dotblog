namespace Lab.ChangeTracking.Domain;

public class PropertyChangeTracker
{
    private readonly Dictionary<string, object> _changedProperties = new();
    private readonly Dictionary<string, object> _originalValues = new();

    public Dictionary<string, object> GetChangedProperties()
    {
        return this._changedProperties;
    }

    public Dictionary<string, object> GetOriginalValues()
    {
        throw new NotImplementedException();
    }

    public void Initial()
    {
        var properties = this.GetType().GetProperties();
        foreach (var property in properties)
        {
            this._originalValues.Add(property.Name, property.GetValue(this));
        }
    }

    public void Track(string propertyName, object value)
    {
        var changes = this._changedProperties;
        var originals = this._originalValues;
        if (changes.ContainsKey(propertyName) == false)
        {
            changes.Add(propertyName, value);
        }
        else
        {
            if (originals[propertyName] != changes[propertyName])
            {
                changes[propertyName] = value;
            }

            if (originals[propertyName] == changes[propertyName])
            {
                changes.Remove(propertyName);
            }
        }
    }
}