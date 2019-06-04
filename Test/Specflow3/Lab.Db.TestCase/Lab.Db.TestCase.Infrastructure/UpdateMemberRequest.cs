using System;
using System.ComponentModel.DataAnnotations;

namespace Lab.Db.TestCase.Infrastructure
{
    public class UpdateMemberRequest
    {
        public Guid Id { get; set; }

        [StringLength(100)]
        [Required]
        public string Name { get; set; }

        public int Age { get; set; }
    }
}