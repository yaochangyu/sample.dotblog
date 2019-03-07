using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UnitTestProject2.Repository.Ef.EntityModel
{
    [Table("Employee")]
    public class Employee
    {
        [Key]
        public Guid Id { get; set; }

        [StringLength(50)]
        public string Name { get; set; }

        public int? Age { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long SequenceId { get; set; }

        [StringLength(50)]
        public string Remark { get; set; }

        public virtual Identity Identity { get; set; }
    }
}