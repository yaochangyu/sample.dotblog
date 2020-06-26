using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EF6.EntityModel
{
    [Table("Member")]
    internal class Member
    {
        [Key]

        public Guid Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; }

        public int Age { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Index("CLIX_Member_SequenceId",IsClustered = true)] 
        public long SequenceId { get; set; }
    }
}