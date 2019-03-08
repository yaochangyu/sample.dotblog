using System;
using System.ComponentModel.DataAnnotations;

namespace Lab.UnitTest
{
    public class EmployeeViewModel
    {
        public Guid Id { get; set; }

        [StringLength(50)]
        public string Name { get; set; }

        public int? Age { get; set; }

        public long SequenceId { get; set; }

        [StringLength(50)]
        public string Remark { get; set; }

        [Required]
        [StringLength(50)]
        public string Account { get; set; }

        [Required]
        [StringLength(50)]
        public string Password { get; set; }
    }
}