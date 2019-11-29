using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lab.DAL.EntityModel
{
    [Table("Identity")]
    public class Identity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public Guid EmployeeId { get; set; }

        public string Account { get; set; }

        public string Password { get; set; }

        public long SequenceId { get; set; }

        public string Remark { get; set; }

        public virtual Employee Employee { get; set; }
    }
}