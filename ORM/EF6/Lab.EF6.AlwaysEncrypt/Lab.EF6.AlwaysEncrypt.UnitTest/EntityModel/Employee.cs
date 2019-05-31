namespace Lab.EF6.AlwaysEncrypt.UnitTest.EntityModel
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("Employee")]
    public partial class Employee
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public Employee()
        {
            Orders = new HashSet<Order>();
        }

        public Guid Id { get; set; }

        [Required]
        [StringLength(10)]
        public string Name { get; set; }

        public int? Age { get; set; }

        public DateTime CreateAt { get; set; }

        public DateTime? ModifyAt { get; set; }

        [Column(TypeName = "numeric")]
        public decimal? Bonus { get; set; }

        [Column(TypeName = "date")]
        public DateTime? Birthday { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int SequenceId { get; set; }

        public virtual Identity Identity { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Order> Orders { get; set; }
    }
}
