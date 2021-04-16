using System.ComponentModel.DataAnnotations;

namespace Lab.DAL.DomainModel.Employee
{
    public class NewRequest
    {
        [Required]
        public string Name { get; set; }

        public int? Age { get; set; }

        [Required]
        public string Account { get; set; }

        [Required]
        public string Password { get; set; }

        public string Remark { get; set; }
    }
}