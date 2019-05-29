using System.Data;
using System.Linq;
using Lab.EF6.DynamicDataMask.UnitTest.EntityModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lab.EF6.DynamicDataMask.UnitTest
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void 切換沒有遮罩帳號()
        {
            using (var dbContext = new TestDbContext())
            {
                if (dbContext.Database.Connection.State == ConnectionState.Closed)
                {
                    dbContext.Database.Connection.Open();
                }

                dbContext.Database.ExecuteSqlCommand("EXECUTE AS USER = 'UnmaskId'");
                var customers = dbContext.Customers.AsNoTracking().ToList();

                dbContext.Database.ExecuteSqlCommand("REVERT");
                Assert.AreEqual("02-77203699", customers[0].Tel);
            }
        }

        [TestMethod]
        public void 切換遮罩帳號()
        {
            using (var dbContext = TestDbContext.CreateDbContext())
            {
                dbContext.Database.ExecuteSqlCommand("EXECUTE AS USER = 'MaskId'");
                var customers = dbContext.Customers.AsNoTracking().ToList();

                dbContext.Database.ExecuteSqlCommand("REVERT");
                Assert.AreEqual("xxxx", customers[0].Tel);
            }
        }

        [TestMethod]
        public void 登入沒有遮罩帳號()
        {
            using (var dbContext = TestDbContext.CreateDbContext("TestDbContext_UnmaskId"))
            {
                var customers = dbContext.Customers.AsNoTracking().ToList();

                Assert.AreEqual("02-77203699", customers[0].Tel);
            }
        }

        [TestMethod]
        public void 登入遮罩帳號()
        {
            using (var dbContext = TestDbContext.CreateDbContext("TestDbContext_MaskId"))
            {
                var customers = dbContext.Customers.AsNoTracking().ToList();
                Assert.AreEqual("xxxx", customers[0].Tel);
            }
        }
    }
}