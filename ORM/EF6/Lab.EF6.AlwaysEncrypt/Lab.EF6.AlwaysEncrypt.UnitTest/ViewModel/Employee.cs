using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lab.EF6.AlwaysEncrypt.UnitTest.ViewModel
{
    public partial class Employee
    {
        //public int Id { get; set; }
        //[Required]
        //[StringLength(10)]
        //public string Name { get; set; }

        //public int? Age { get; set; }

        //public DateTime CreateAt { get; set; }

        //public DateTime ModifyAt { get; set; }

        //public decimal Bonus { get; set; }

        //=============================================
        public int Id { get; set; }

        [Required]
        [StringLength(10)]
        public string Name { get; set; }

        public int? Age { get; set; }

        public DateTime CreateAt { get; set; }

        public DateTime? ModifyAt { get; set; }

        [Column(TypeName = "numeric")]
        public decimal? Bonus { get; set; }

        public DateTime? BirthDay { get; set; }
    }
}
