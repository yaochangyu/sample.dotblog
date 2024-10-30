namespace Lab.Sharding.Infrastructure;

public interface IUuidProvider
{
    public string NewId();
}

public class UuidProvider : IUuidProvider
{
    public string NewId() => Guid.NewGuid().ToString();
}