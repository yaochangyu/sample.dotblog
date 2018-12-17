using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTestProject1
{
    [TestClass]
    public class ValidatableObjectTests
    {
        [TestMethod]
        public void TryValidateObject_Test()
        {
            var contact = new Contact
            {
                FirstName = "Armin",
                LastName = "Zia",
                EMail = "mail"
            };
            var context = new ValidationContext(contact, null, null);
            var errors = new List<ValidationResult>();
            if (!Validator.TryValidateObject(contact, context, errors, true))
            {
                Assert.AreEqual(1, errors.Count);
            }
        }

        [TestMethod]
        public void ValidateObject_Test()
        {
            var contact = new Contact
            {
                FirstName = "Armin",
                LastName = "Zia",
                EMail = "mail"
            };
            var context = new ValidationContext(contact, null, null);
            Action action = () => Validator.ValidateObject(contact, context, true);
            action.Should().Throw<ValidationException>();
        }

        private class Contact : IValidatableObject
        {
            [Required]
            public int Id { get; set; }

            [Required(AllowEmptyStrings = false, ErrorMessage = "First name is required")]
            public string FirstName { get; set; }

            public string LastName { get; set; }

            public DateTime Birthday { get; set; }

            public string EMail { get; set; }

            public DateTime StartDate { get; } = DateTime.Parse("3000-01-01");

            public DateTime EndDate { get; set; }

            public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
            {
                int result = DateTime.Compare(this.StartDate, this.EndDate);
                if (result >= 0)
                {
                    yield return new ValidationResult("start date must be less than the end date!",
                                                      new[] {"ConfirmEmail"});
                }
            }
        }
    }
}