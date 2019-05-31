namespace Lab.EF6.AlwaysEncrypt.UnitTest.EntityModel
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("Order")]
    public partial class Order
    {
        public Guid Id { get; set; }

        public Guid? Employee_Id { get; set; }

        [Required]
        [StringLength(50)]
        public string ProductName { get; set; }

        [Column(TypeName = "numeric")]
        public decimal Price { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int SequenceId { get; set; }

        public virtual Employee Employee { get; set; }
    }
}
