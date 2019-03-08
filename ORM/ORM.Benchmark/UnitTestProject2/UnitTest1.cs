using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core.Mapping;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Infrastructure;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UnitTestProject2.Repository.Ef.EntityModel;
using UnitTestProject2.Repository.Linq2Db;

namespace UnitTestProject2
{
    [TestClass]
    public class UnitTest1
    {
        private static IEnumerable<TestReport> Reports;

        public UnitTest1()
        {
            string connectionName = "LabDbContext";
            if (Reports == null)
            {
                Reports = this.CreateTestReports();
            }

            Database.SetInitializer<LabDbContext>(null);
            using (var dbcontext = new LabDbContext(connectionName))
            {
                var objectContext = ((IObjectContextAdapter) dbcontext).ObjectContext;
                var mappingCollection =
                    (StorageMappingItemCollection) objectContext.MetadataWorkspace.GetItemCollection(DataSpace.CSSpace);
                mappingCollection.GenerateViews(new List<EdmSchemaError>());
            }

            foreach (var repository in Utility.Repositories)
            {
                int count;
                repository.Value.GetAllEmployees(out count);
            }
        }

        [TestMethod]
        public void TestMethod1()
        {
            //var repository = new EfNoTrackEmployeeRepository("LabDbContext");
            var repository = new Linq2EmployeeRepository("LabDbContext");

            //var repository = new DapperEmployeeRepository("LabDbContext");
            //var repository = new DataReaderEmployeeRepository("LabDbContext");

            int count;
            var employeesFromDb = repository.GetAllEmployeesDetail(out count);

            //Assert.IsTrue(employeesFromDb.Count() > 0);
        }

        [TestMethod]
        public void TestMethod2()
        {
            string connectionName = "LabDbContext";
            this.Run(Reports, 5);
        }

        private void Run(IEnumerable<TestReport> reports, int runTime)
        {
            foreach (var report in reports)
            {
                report.RunAllEmployees(runTime);
            }

            var sortTestReports = reports.OrderBy(p => p.TotalCostTime).ToList();

            for (var i = 0; i < sortTestReports.Count; i++)
            {
                var report = sortTestReports[i];
                Console.WriteLine($"第 {i + 1} 名，{report.TotalTestInfo}");
            }

            Console.WriteLine();
            for (var i = 0; i < sortTestReports.Count; i++)
            {
                var report = sortTestReports[i];
                Console.WriteLine($"第 {i + 1} 名，{report.TestInfo}");
            }
        }

        private IEnumerable<TestReport> CreateTestReports()
        {
            var reports = new HashSet<TestReport>();
            foreach (var repository in Utility.Repositories)
            {
                var testReport = new TestReport(repository.Key.ToString(),
                                                () =>
                                                {
                                                    int count = 0;
                                                    var value = repository.Value;
                                                    value.GetAllEmployees(out count);
                                                    return new DataInfo {RowCount = count};
                                                });
                reports.Add(testReport);
            }

            foreach (var repository in Utility.AdoRepositories)
            {
                var testReport = new TestReport(repository.Key.ToString(),
                                                () =>
                                                {
                                                    int count = 0;
                                                    var value = repository.Value;
                                                    value.GetAllEmployees(out count);
                                                    return new DataInfo {RowCount = count};
                                                });
                reports.Add(testReport);
            }

            return reports;
        }
    }
}