using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Lab.ChangeTracking.Abstract;

namespace Lab.ChangeTracking.Infrastructure.DB.EntityModel
{
    public class Employee : IEntity
    {
        public Guid Id { get; set; }

        public int Version { get; set; }

        [Required]
        public string Name { get; set; }

        public int? Age { get; set; }

        public IList<Address> Addresses { get; set; }

        public Identity Identity { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long SequenceId { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public string CreatedBy { get; set; }

        public DateTimeOffset? ModifiedAt { get; set; }

        public string ModifiedBy { get; set; }

        public string Remark { get; set; }
    }
}