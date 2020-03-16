using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UnitTestProject1.EntityModel
{
    [Table("Identity")]
    public class Identity
    {
        [Index("UK_Account",IsUnique = true)]
        public string Account { get; set; }

        public string Password { get; set; }

        [ForeignKey("Employee")]
        [Key]
        public Guid Employee_Id { get; set; }

        public virtual Employee Employee { get; set; }
    }
}