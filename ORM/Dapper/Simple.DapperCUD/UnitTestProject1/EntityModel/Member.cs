using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Dapper.Contrib.Extensions;

namespace UnitTestProject1.EntityModel
{
    [System.ComponentModel.DataAnnotations.Schema.Table("Member")]
    internal class Member
    {
        [System.ComponentModel.DataAnnotations.Key]
        [ExplicitKey]
        public Guid Id { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Computed]
        public long SequenceId { get; set; }

        [StringLength(100)]
        public string Name { get; set; }

        public int Age { get; set; }
    }
}