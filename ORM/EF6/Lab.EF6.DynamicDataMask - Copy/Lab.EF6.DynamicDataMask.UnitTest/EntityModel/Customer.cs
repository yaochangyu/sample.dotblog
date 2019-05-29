namespace Lab.EF6.DynamicDataMask.UnitTest.EntityModel
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("Customer")]
    public partial class Customer
    {
        [StringLength(11)]
        public string ID { get; set; }

        [StringLength(10)]
        public string Name { get; set; }

        [Column(TypeName = "date")]
        public DateTime? Birthday { get; set; }

        [StringLength(1)]
        public string Marriage { get; set; }

        [StringLength(50)]
        public string Email { get; set; }

        [StringLength(20)]
        public string Tel { get; set; }

        [Column(TypeName = "numeric")]
        public decimal? Salary { get; set; }

        [StringLength(19)]
        public string CreditCard { get; set; }
    }
}
