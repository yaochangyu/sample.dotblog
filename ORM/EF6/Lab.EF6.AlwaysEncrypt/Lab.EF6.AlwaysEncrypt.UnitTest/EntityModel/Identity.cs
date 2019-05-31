namespace Lab.EF6.AlwaysEncrypt.UnitTest.EntityModel
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("Identity")]
    public partial class Identity
    {
        [Key]
        public Guid Employee_Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Account { get; set; }

        [Required]
        [StringLength(50)]
        public string Password { get; set; }

        public virtual Employee Employee { get; set; }
    }
}
