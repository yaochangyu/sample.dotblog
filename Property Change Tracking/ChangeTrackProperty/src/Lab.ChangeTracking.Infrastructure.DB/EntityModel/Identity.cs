using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lab.ChangeTracking.Infrastructure.DB.EntityModel
{
    public class Identity
    {
        public Guid Employee_Id { get; set; }

        // [ForeignKey("Employee_Id")]
        public virtual Employee Employee { get; set; }

        [Required]
        public string Account { get; set; }

        [Required]
        public string Password { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long SequenceId { get; set; }

        public string Remark { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public string CreatedBy { get; set; }

        public DateTimeOffset? ModifiedAt { get; set; }

        public string? ModifiedBy { get; set; }
    }
}