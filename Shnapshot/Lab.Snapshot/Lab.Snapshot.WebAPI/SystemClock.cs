namespace Lab.Snapshot.WebAPI;

public class SystemClock: ISystemClock
{
    public DateTimeOffset Now => DateTimeOffset.UtcNow;
}