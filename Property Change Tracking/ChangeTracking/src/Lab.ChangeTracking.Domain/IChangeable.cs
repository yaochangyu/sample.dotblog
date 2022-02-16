namespace Lab.ChangeTracking.Domain.Annotations;

public interface IChangeable
{
    Dictionary<string, object> GetChangedProperties();

    Dictionary<string, object> GetOriginalValues();

    void Track(string propertyName, object value);

    void Initial();

    bool HasChanged { get; }
}