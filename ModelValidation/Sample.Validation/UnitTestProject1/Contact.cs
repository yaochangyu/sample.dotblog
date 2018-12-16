using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace UnitTestProject1
{
    public class Contact : IValidatableObject
    {
        [Required]
        public int Id { get; set; }

        [Required(AllowEmptyStrings = false, ErrorMessage = "First name is required")]
        public string FirstName { get; set; }

        public string LastName { get; set; }

        public DateTime Birthday { get; set; }

        public string EMail { get; set; }

        public DateTime StartDate { get; set; } = DateTime.Parse("3000-01-01");

        public DateTime EndDate { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            int result = DateTime.Compare(this.StartDate, this.EndDate);
            if (result >= 0)
            {
                yield return new ValidationResult("start date must be less than the end date!", new[] {"ConfirmEmail"});
            }
        }
    }

    public class Contact2
    {
        [Required]
        public int Id { get; set; }

        public DateTime StartDate { get; set; }

        [GreaterThan("StartDate")]
        public DateTime EndDate { get; set; }
    }
}