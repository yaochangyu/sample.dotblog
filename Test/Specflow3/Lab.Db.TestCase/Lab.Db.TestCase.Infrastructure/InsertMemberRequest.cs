using System;
using System.ComponentModel.DataAnnotations;

namespace Lab.Db.TestCase.Infrastructure
{
    public class InsertMemberRequest
    {
        [StringLength(100)]
        [Required]
        public string Name { get; set; }

        public int Age { get; set; }

    }
}