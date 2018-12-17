using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTestProject1
{
    [TestClass]
    public class ValidationAttributeTests
    {
        [TestMethod]
        public void GreaterThan_Test()
        {
            var contact = new Contact2
            {
                StartDate = DateTime.Parse("2000,1,1"),
                EndDate = DateTime.Parse("1999,1,1")
            };
            var context = new ValidationContext(contact, null, null);
            var errors = new List<ValidationResult>();
            if (!Validator.TryValidateObject(contact, context, errors, true))
            {
                Assert.AreEqual(1, errors.Count);
            }
        }

        private class Contact2
        {
            [Required]
            public int Id { get; set; }

            public DateTime StartDate { get; set; }

            [GreaterThan("StartDate")]
            public DateTime EndDate { get; set; }
        }
    }
}