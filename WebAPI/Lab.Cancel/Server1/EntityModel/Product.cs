using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Server1.EntityModel
{
    [Table("Product")]
    public class Product
    {
        [Key]
        public Guid Id { get; set; }

        [StringLength(100)]
        public string Name { get; set; }

    }
}