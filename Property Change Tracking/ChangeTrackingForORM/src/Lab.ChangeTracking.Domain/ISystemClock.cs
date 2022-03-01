namespace Lab.ChangeTracking.Domain;

public interface ISystemClock
{
    DateTimeOffset Now { get; set; }
}

public class SystemClock : ISystemClock
{
    public DateTimeOffset Now { get; set; }=DateTimeOffset.Now;
}