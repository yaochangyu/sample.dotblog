namespace Lab.ChangeTracking.Abstract;

public interface IChangeState
{
    int Version { get; set; }
}