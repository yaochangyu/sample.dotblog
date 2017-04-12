using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAL
{
    [Table("MemberLog")]
    public class MemberLog
    {
        [Key]
        public Guid Id { get; set; }

        //[Required]
        [StringLength(100)]
        public string Name { get; set; }

        public virtual Member Member { get; set; }
        public int? Age { get; internal set; }
        public DateTime Birthday { get; set; }

        [StringLength(100)]
        [Required]
        public string UserId { get; set; }
    }
}