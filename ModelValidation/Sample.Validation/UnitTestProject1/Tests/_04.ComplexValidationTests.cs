using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTestProject1
{
    [TestClass]
    public class ComplexValidationTests
    {
        [TestMethod]
        public void ComplexValidation_Test()
        {
            var contact = new Contact
            {
                Id = 1,
                StartDate = DateTime.Parse("2000,1,1"),
                EndDate = DateTime.Parse("1999,1,1"),
                Logs = new List<ContactLog>
                {
                    new ContactLog {Id = 1, Contact_Id = 1}
                }
            };

            var validationResults = new List<ValidationResult>();
            var isValid = contact.TryValidate2(validationResults);
            Assert.AreEqual(false, isValid);
        }

        private class Contact
        {
            [Required]
            public int Id { get; set; }

            public DateTime StartDate { get; set; }

            [GreaterThan("StartDate")]
            public DateTime EndDate { get; set; }

            [ComplexValidation]
            public IEnumerable<ContactLog> Logs { get; set; }
        }

        private class ContactLog
        {
            public int Id { get; set; }

            public int Contact_Id { get; set; }

            [Required]
            public string Name { get; set; }

            public DateTime CreateAt { get; set; }
        }
    }
}