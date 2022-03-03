using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Lab.ChangeTracking.Abstract;

namespace Lab.ChangeTracking.Infrastructure.DB.EntityModel
{
    public class Employee : IEmployeeEntity
    {
        public Guid Id { get; set; }

        [Required]
        public string Name { get; set; }

        public int? Age { get; set; }

        public string Remark { get; set; }

        public List<Profile> Profiles { get; set; } = new();

        public Identity Identity { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long SequenceId { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public string CreatedBy { get; set; }

        public DateTimeOffset UpdatedAt { get; set; }

        public string UpdatedBy { get; set; }

        public int Version { get; set; }
    }
}