namespace Lab.ChangeTracking.Abstract;

public interface IProfileEntity : IChangeTime
{
    public string FirstName { get; set; }

    public string LastName { get; set; }

    public string Remark { get; set; }
}