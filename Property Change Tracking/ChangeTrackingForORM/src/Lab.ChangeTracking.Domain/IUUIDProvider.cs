namespace Lab.ChangeTracking.Domain;

public interface IUUIdProvider
{
    Guid GenerateId();
}

public class UUIdProvider : IUUIdProvider
{
    public Guid GenerateId()
    {
        return Guid.NewGuid();
    }
}