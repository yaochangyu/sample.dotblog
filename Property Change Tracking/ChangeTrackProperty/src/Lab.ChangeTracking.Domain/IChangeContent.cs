namespace Lab.ChangeTracking.Domain;

public interface IChangeContent
{
    Dictionary<string, object> GetChangedProperties();

    Dictionary<string, object> GetOriginalValues();
}