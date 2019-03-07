using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using UnitTestProject2.Repository.Ef;
using UnitTestProject2.Repository.Ef.EntityModel;

namespace UnitTestProject2.ViewModel
{
    public class EmployeeViewModel
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public EmployeeViewModel()
        {
            Identities = new HashSet<Identity>();
        }

        public Guid Id { get; set; }

        [StringLength(50)]
        public string Name { get; set; }

        public int? Age { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long SequenceId { get; set; }

        [StringLength(50)]
        public string Remark { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Identity> Identities { get; set; }

        [Required]
        [StringLength(50)]
        public string Account { get; set; }

        [Required]
        [StringLength(50)]
        public string Password { get; set; }
    }
}