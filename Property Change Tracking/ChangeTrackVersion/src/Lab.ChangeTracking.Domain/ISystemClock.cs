namespace Lab.ChangeTracking.Domain;

public interface ISystemClock
{
    DateTimeOffset GetNow();
}

public class SystemClock : ISystemClock
{
    public DateTimeOffset GetNow()
    {
        return DateTimeOffset.Now;
    }
}