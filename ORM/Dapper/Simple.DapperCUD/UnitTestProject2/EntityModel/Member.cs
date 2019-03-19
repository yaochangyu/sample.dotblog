using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LinqToDB.Mapping;

namespace UnitTestProject2.EntityModel
{
    [System.ComponentModel.DataAnnotations.Schema.Table("Member")]
    public class Member
    {
        [System.ComponentModel.DataAnnotations.Key]
        [PrimaryKey]
        public Guid Id { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Identity]
        public long SequenceId { get; set; }

        [StringLength(100)]
        public string Name { get; set; }

        public int Age { get; set; }
    }
}