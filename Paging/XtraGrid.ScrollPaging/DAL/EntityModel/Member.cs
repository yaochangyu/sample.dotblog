using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAL
{
    [Table("Member")]
    public class Member
    {
        [Key]
        public Guid Id { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int SequentialId { get; set; }

        [StringLength(100)]
        public string Name { get; set; }

        public int? Age { get; set; }

        public DateTime? Birthday { get; set; }

        [Required]

        //[Required(AllowEmptyStrings = true)]
        [StringLength(100)]
        public string UserId { get; set; }

        public virtual ICollection<MemberLog> MemberLogs { get; set; }
    }
}