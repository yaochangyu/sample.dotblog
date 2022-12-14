using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lab.StepDependencyInjection.Test.EntityModel
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

        [Required]
        public DateTimeOffset CreateAt { get; set; }

        [Required]
        public string CreateBy { get; set; }
    }
}