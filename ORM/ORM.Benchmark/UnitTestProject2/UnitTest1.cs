using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using UnitTestProject2.Repository.Ado;
using UnitTestProject2.Repository.Dapper;
using UnitTestProject2.Repository.Ef;
using UnitTestProject2.Repository.Linq2Db;

namespace UnitTestProject2
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            //var repository = new EfNoTrackEmployeeRepository("LabDbContext");
            var repository = new Linq2EmployeeRepository("LabDbContext");
            //var repository = new DapperEmployeeRepository("LabDbContext");
            //var repository = new DataReaderEmployeeRepository("LabDbContext");

            int count;
            var employeesFromDb = repository.GetAllEmployees(out count);
            //Assert.IsTrue(employeesFromDb.Count() > 0);
        }
    }
}