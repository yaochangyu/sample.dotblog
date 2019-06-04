using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lab.Db.TestCase.DAL
{
    [Table("Member")]
    public class Member
    {
        [Key]
        public Guid Id { get; set; }

        [StringLength(100)]
        public string Name { get; set; }

        public int Age { get; set; }

        [Required]
        public DateTime CreateAt { get; set; }

        [StringLength(100)]
        [Required]
        public string CreateBy { get; set; }

        [StringLength(200)]
        public string Remark { get; set; }

        public DateTime? ModifyAt { get; set; }

        [StringLength(100)]
        public string ModifyBy { get; set; }
    }
}