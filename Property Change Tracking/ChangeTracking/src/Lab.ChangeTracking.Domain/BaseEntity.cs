using System.Reflection;

namespace Lab.ChangeTracking.Domain.Annotations;

public record BaseEntity : IChangeable
{
    private PropertyChangeTracker _tracker = new();

    public void Initial()
    {
        this._tracker.Initial();
    }

    public bool HasChanged { get; private set; }

    public Dictionary<string, object> GetChangedProperties()
    {
        return this._tracker.GetChangedProperties();
    }

    public Dictionary<string, object> GetOriginalValues()
    {
        throw new NotImplementedException();
    }

    public void Track(string propertyName, object value)
    {
        this._tracker.Track(propertyName, value);
    }
}