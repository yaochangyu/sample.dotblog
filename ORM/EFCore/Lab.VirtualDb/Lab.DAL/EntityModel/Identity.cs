using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lab.DAL.EntityModel
{
    [Table("Identity")]
    public class Identity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid EmployeeId { get; set; }

        [Required]
        public string Account { get; set; }

        [Required]
        public string Password { get; set; }

        public long SequenceId { get; set; }

        public string Remark { get; set; }

        public virtual Employee Employee { get; set; }
    }
}