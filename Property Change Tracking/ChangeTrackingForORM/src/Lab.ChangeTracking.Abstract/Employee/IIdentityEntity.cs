namespace Lab.ChangeTracking.Abstract;

public interface IIdentityEntity : IChangeTime
{
    public string Account { get; set; }

    public string Password { get; set; }

    public string Remark { get; set; }
}