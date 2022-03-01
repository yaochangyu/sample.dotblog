using Lab.ChangeTracking.Abstract;

namespace Lab.ChangeTracking.Infrastructure.DB.EntityModel;

public class Profile : IProfileEntity
{
    public Guid Id { get; set; }

    public Guid Employee_Id { get; set; }

    public Employee Employee { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public string CreatedBy { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public string UpdatedBy { get; set; }

    public string FirstName { get; set; }

    public string LastName { get; set; }

    public string Remark { get; set; }

    public int SequenceId { get; set; }
}