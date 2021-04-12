using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lab.DAL.EntityModel
{
    [Table("Identity")]
    public class Identity
    {
        [Key]
<<<<<<< HEAD
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid EmployeeId { get; set; }

        [Required]
        public string Account { get; set; }

        [Required]
        public string Password { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
=======
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public Guid EmployeeId { get; set; }

        public string Account { get; set; }

        public string Password { get; set; }

>>>>>>> origin/master
        public long SequenceId { get; set; }

        public string Remark { get; set; }

        public virtual Employee Employee { get; set; }
    }
}