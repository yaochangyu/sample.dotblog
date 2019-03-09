using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core.Mapping;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Infrastructure;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UnitTestProject2.Repository.Ef.EntityModel;

namespace UnitTestProject2
{
    [TestClass]
    public class TestReportTests
    {
        private static readonly IEnumerable<TestReport> Reports;
        private static readonly IEnumerable<TestReport> JoinReports;
        private static readonly string ConnectionName = "LabDbContext";

        static TestReportTests()
        {
            if (Reports == null)
            {
                Reports = CreateTestReports();
            }

            if (JoinReports == null)
            {
                JoinReports = CreateJoinTestReports();
            }

            //不檢查Migration
            Database.SetInitializer<LabDbContext>(null);

            //載入Model Mapping Table
            using (var dbcontext = new LabDbContext(ConnectionName))
            {
                var objectContext = ((IObjectContextAdapter)dbcontext).ObjectContext;
                var mappingCollection =
                    (StorageMappingItemCollection)objectContext.MetadataWorkspace.GetItemCollection(DataSpace.CSSpace);
                mappingCollection.GenerateViews(new List<EdmSchemaError>());
            }

            //暖機
            foreach (var report in Reports)
            {
                report.Run(1);
            }

            foreach (var report in JoinReports)
            {
                report.Run(1);
            }
        }

        [TestMethod]
        public void TestMethod1()
        {
            //var repository = new EfNoTrackEmployeeRepository("LabDbContext");
            //var repository = new Linq2EmployeeRepository("LabDbContext");

            //var repository = new DapperEmployeeRepository("LabDbContext");
            //var repository = new DataReaderEmployeeRepository("LabDbContext");

            //int count;
            //var employeesFromDb = repository.GetAllEmployeesDetail(out count);

            //Assert.IsTrue(employeesFromDb.Count() > 0);
        }

        [TestMethod]
        public void Benchmark()
        {
            string connectionName = "LabDbContext";
            this.Run(Reports, 1);
        }

        [TestMethod]
        public void JoinBenchmark()
        {
            string connectionName = "LabDbContext";
            this.Run(JoinReports, 1);
        }

        private void Run(IEnumerable<TestReport> reports, int runTime)
        {
            foreach (var report in reports)
            {
                report.Run(runTime);
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

        private static IEnumerable<TestReport> CreateTestReports()
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
                                                    return new DataInfo { RowCount = count };
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
                                                    return new DataInfo { RowCount = count };
                                                });
                reports.Add(testReport);
            }

            return reports;
        }

        private static IEnumerable<TestReport> CreateJoinTestReports()
        {
            var reports = new HashSet<TestReport>();
            foreach (var repository in Utility.Repositories)
            {
                var testReport = new TestReport(repository.Key.ToString(),
                                                () =>
                                                {
                                                    int count = 0;
                                                    var value = repository.Value;
                                                    value.GetAllEmployeesDetail(out count);
                                                    return new DataInfo { RowCount = count };
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
                                                    value.GetAllEmployeesDetail(out count);
                                                    return new DataInfo { RowCount = count };
                                                });
                reports.Add(testReport);
            }

            return reports;
        }
    }
}