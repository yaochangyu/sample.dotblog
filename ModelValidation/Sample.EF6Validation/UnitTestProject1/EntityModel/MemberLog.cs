using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UnitTestProject1.EntityModel
{
    internal class MemberLog
    {
        [Key]
        public Guid Id { get; set; }

        [ForeignKey("Member")]
        public Guid Memebr_Id { get; set; }

        [StringLength(100)]
        public string Name { get; set; }

        public virtual Member Member { get; set; }

        //public DateTime CreateAt { get; set; }
    }
}