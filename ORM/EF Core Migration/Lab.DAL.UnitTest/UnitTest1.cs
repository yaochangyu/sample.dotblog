using System.Linq;
using System.Linq.Expressions;
using Lab.DAL.EntityModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lab.DAL.UnitTest
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            using (var dbContext=new LabEmployeeContext(DbOptionsFactory.DbContextOptions))
            {
                var employees = dbContext.Employee.AsNoTracking().ToList();
            }
        }
    }
}
