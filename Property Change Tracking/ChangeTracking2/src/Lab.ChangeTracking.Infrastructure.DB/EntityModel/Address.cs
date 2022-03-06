using System.ComponentModel.DataAnnotations.Schema;

namespace Lab.ChangeTracking.Infrastructure.DB.EntityModel;

public class Address
{
    public Guid Id { get; set; }

    public Guid Employee_Id { get; set; }

    public Employee Employee { get; set; }

    public string Country { get; set; }

    public string Street { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public string CreatedBy { get; set; }

    public DateTimeOffset? ModifiedAt { get; set; }

    public string ModifiedBy { get; set; }

    public string Remark { get; set; }

    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int SequenceId { get; set; }
}