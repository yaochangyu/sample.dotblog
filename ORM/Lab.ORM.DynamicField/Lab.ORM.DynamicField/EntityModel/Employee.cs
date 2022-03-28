using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lab.ORM.DynamicField.EntityModel
{
    [Table("Employee")]
    public class Employee
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public Guid Id { get; set; }

        [Required]
        public string Name { get; set; }

        public int? Age { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long SequenceId { get; set; }

        public string Remark { get; set; }

        // public virtual Identity Identity { get; set; }

        [Required]
        public DateTimeOffset CreatedAt { get; set; }

        [Required]
        public string CreatedBy { get; set; }

        public DateTimeOffset? ModifiedAt { get; set; }

        public string ModifiedBy { get; set; }
    }
}