using System;
using System.Diagnostics;
using System.Linq;
using Lab.EF6.DynamicDataMask.UnitTest.EntityModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lab.EF6.DynamicDataMask.UnitTest
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            using (var dbContext=new TestDbContext())
            {
                var customers = dbContext.Customers.AsNoTracking().ToList();
                Console.WriteLine(customers[0].Tel);
            }
        }
    }
}
