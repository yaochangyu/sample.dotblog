using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UnitTestProject2.Repository.Ef.EntityModel
{
    [Table("Identity")]
    public class Identity
    {
        [Key]
        [ForeignKey("Employee")]
        [Required]
        public Guid Employee_Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Account { get; set; }

        [Required]
        [StringLength(50)]
        public string Password { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long SequenceId { get; set; }

        [StringLength(50)]
        public string Remark { get; set; }

        public virtual Employee Employee { get; set; }
    }
}