using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lab.EF6.AlwaysEncrypt.UnitTest.ViewModel
{
    public class Employee
    {
        private string _name;

        public Guid Id { get; set; }

        [Required]
        [StringLength(10)]
        public string Name
        {
            get
            {
                if (this._name == "小章")
                {
                    this._name = "yao";
                }

                return this._name;
            }
            set => this._name = value;
        }

        public int? Age { get; set; }

        public DateTime CreateAt { get; set; }

        public DateTime? ModifyAt { get; set; }

        [Column(TypeName = "numeric")]
        public decimal? Bonus { get; set; }

        public DateTime? BirthDay { get; set; }
    }
}