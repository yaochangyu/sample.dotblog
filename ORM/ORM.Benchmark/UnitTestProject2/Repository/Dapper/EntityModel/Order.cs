using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UnitTestProject2.Repository.Dapper.EntityModel
{
    [Table("Order")]
    public class Order
    {
        public Guid Id { get; set; }

        public Guid? Employee_Id { get; set; }

        public DateTime? OrderTime { get; set; }

        [StringLength(50)]
        public string Remark { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long SequenceId { get; set; }
    }
}