namespace Lab.Snapshot.WebAPI;

public interface ISystemClock
{ 
    DateTimeOffset Now { get; } 
}