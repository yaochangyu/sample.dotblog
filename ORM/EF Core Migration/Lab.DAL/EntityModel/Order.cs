using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lab.DAL.EntityModel
{
    [Table("Order")]
    public class Order
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public Guid Id { get; set; }

        public Guid? EmployeeId { get; set; }

        public DateTime? OrderTime { get; set; }

        public string Remark { get; set; }

        public long SequenceId { get; set; }

        public virtual Employee Employee { get; set; }
    }
}