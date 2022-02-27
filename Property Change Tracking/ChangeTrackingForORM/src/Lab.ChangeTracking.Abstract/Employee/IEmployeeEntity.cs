namespace Lab.ChangeTracking.Abstract;

public interface IEmployeeEntity : IChangeTrackable
{
    public Guid Id { get; set; }

    public string Name { get; set; }

    public int? Age { get; set; }

    public string Remark { get; set; }

    // public IList<IProfileEntity> Profiles { get; set; }
    //
    // public IIdentityEntity Identity { get; set; }

}