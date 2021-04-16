using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lab.SQLite.EntityModel
{
    [Table("OrderHistory")]
    public class OrderHistory
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public string Id { get; set; }

        public string Employee_Id { get; set; }

        public string Remark { get; set; }
        
        public string Product_Id { get; set; }

        public string Product_Name { get; set; }

        [Required]
        public DateTime CreateAt { get; set; }

        [Required]
        public string CreateBy { get; set; }
    }
}