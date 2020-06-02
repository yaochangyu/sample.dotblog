using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lab.DAL.EntityModel
{
    [Table("Employee")]

    public class Employee
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public Guid Id { get; set; }

        public string Name { get; set; }

        public int? Age { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long SequenceId { get; set; }

        public string Remark { get; set; }

        public virtual Identity Identity { get; set; }

        public virtual ICollection<Order> Order { get; set; }

        public Employee()
        {
            this.Order = new HashSet<Order>();
        }
    }
}