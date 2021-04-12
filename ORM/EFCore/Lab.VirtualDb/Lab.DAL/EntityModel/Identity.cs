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
        public Guid Employee_Id { get; set; }

        [Required]
        public string Account { get; set; }

        [Required]
        public string Password { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long SequenceId { get; set; }
        public string Remark { get;   set; }

        [ForeignKey("Employee_Id")]
        public virtual Employee Employee { get; set; }
    }
}