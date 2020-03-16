using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UnitTestProject1.EntityModel
{
    [Table("Employee")]
    public class Employee
    {
        [Required]
        [Key]
        public Guid Id { get; set; }

        [StringLength(100)]
        public string Name { get; set; }

        public int Age { get; set; }

        public virtual Identity Identity { get; set; }  
    }
}