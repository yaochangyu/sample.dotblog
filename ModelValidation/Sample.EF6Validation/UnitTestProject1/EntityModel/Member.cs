using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UnitTestProject1.EntityModel
{
    [Table("Member")]
    internal class Member
    {
        public Member()
        {
            this.Logs = new HashSet<MemberLog>();
        }

        [Key]
        public Guid Id { get; set; }

        [StringLength(100)]
        public string Name { get; set; }

        public DateTime CreateAt { get; set; }

        public virtual ICollection<MemberLog> Logs { get; set; }
    }
}