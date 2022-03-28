using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace Lab.ORM.DynamicField.EntityModel
{
    [Table("Employee")]
    public class Employee : IDisposable
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public Guid Id { get; set; }

        [Required]
        public string Name { get; set; }

        public int? Age { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long SequenceId { get; set; }

        public string Remark { get; set; }

        public JsonDocument Profiles { get; set; }

        // [NotMapped]
        public Customer Customer
        {
            get;
            set;

            // get => _customer == null ? null : JsonSerializer.Deserialize<Customer>(_customer);
            // set => _customer = JsonSerializer.Serialize(value);
        }

        internal string _customer;

        [Required]
        public DateTimeOffset CreatedAt { get; set; }

        [Required]
        public string CreatedBy { get; set; }

        public DateTimeOffset? ModifiedAt { get; set; }

        public string ModifiedBy { get; set; }

        public void Dispose() => this.Profiles?.Dispose();
    }
}