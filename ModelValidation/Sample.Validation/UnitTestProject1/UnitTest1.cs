using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTestProject1
{
    [TestClass]
    public class UnitTest1
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


        [TestMethod]
        public void GreaterThan_Test()
        {
            var contact = new Contact2
            {
                StartDate = DateTime.Parse("2000,1,1"),
                EndDate = DateTime.Parse("1999,1,1"),
            };
            var context = new ValidationContext(contact, null, null);
            var errors = new List<ValidationResult>();
            if (!Validator.TryValidateObject(contact, context, errors, true))
            {
                Assert.AreEqual(1, errors.Count);
            }
        }

    }
}
