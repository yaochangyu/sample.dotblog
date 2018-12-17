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
        public void GreaterThan_Test()
        {
            var contact = new Contact
            {
                StartDate = DateTime.Parse("2000,1,1"),
                EndDate = DateTime.Parse("1999,1,1")
            };
            //contact.TryValidate();
           
        }

        public class Contact
        {
            [Required]
            public int Id { get; set; }

            public DateTime StartDate { get; set; }

            [GreaterThan("StartDate")]
            public DateTime EndDate { get; set; }
        }
    }
}