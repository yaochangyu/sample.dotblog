using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lab.SQLite.EntityModel
{
    [Table("Employee")]
    public class Employee
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public string Id { get; set; }

        [Required]
        public string Name { get; set; }

        public int? Age { get; set; }

        public string Remark { get; set; }

        [Required]
        public DateTime CreateAt { get; set; }

        [Required]
        public string CreateBy { get; set; }

        public virtual Identity Identity { get; set; }
    }
}