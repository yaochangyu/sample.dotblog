using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lab.Infrastructure.DB.EntityModel
{
    [Table("OrderHistory")]
    public class OrderHistory
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        public Guid? Employee_Id { get; set; }

        public string Remark { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long SequenceId { get; set; }

        public string Product_Id { get; set; }

        public string Product_Name { get; set; }

        [Required]
        public DateTime CreateAt { get; set; }

        [Required]
        public string CreateBy { get; set; }
    }
}