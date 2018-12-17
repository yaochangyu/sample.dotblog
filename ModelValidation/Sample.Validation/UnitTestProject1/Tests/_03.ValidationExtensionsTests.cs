using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTestProject1
{
    [TestClass]
    public class ValidationExtensionsTests
    {
        [TestMethod]
        public void TryValidate_Test()
        {
            var contact = new Contact
            {
                StartDate = DateTime.Parse("2000,1,1"),
                EndDate = DateTime.Parse("1999,1,1")
            };

            var validationResults = new List<ValidationResult>();
            var isValid = contact.TryValidate(validationResults);
            Assert.AreEqual(false, isValid);

        }

        [TestMethod]
        public void GIVEN_Collection_WHEN_Call_TryValidate_THEN_Valid_Faild_Count_Be_2()
        {
            var contacts = new List<Contact>
            {
                new Contact
                {
                    StartDate = DateTime.Parse("2000,1,1"),
                    EndDate = DateTime.Parse("1999,1,1")
                }
            };
            var contact = new Contact
            {
                StartDate = DateTime.Parse("2000,1,1"),
                EndDate = DateTime.Parse("1999,1,1")
            };

            var validationResults = new List<ValidationResult>();
            var validCollection = contacts.TryValidate(validationResults);
            var validInstance = contact.TryValidate(validationResults);
            var isValid = validCollection & validInstance;
            Assert.AreEqual(false, isValid);
            Assert.AreEqual(2, validationResults.Count);
        }

        private class Contact
        {
            [Required]
            public int Id { get; set; }

            public DateTime StartDate { get; set; }

            [GreaterThan("StartDate")]
            public DateTime EndDate { get; set; }
        }
    }
}