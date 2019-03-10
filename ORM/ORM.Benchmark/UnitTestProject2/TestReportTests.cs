using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTestProject2
{
    [TestClass]
    public class TestReportTests
    {
        private static readonly IEnumerable<TestReport> Reports;
        private static readonly IEnumerable<TestReport> JoinReports;

        private readonly int _time = 10;

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
        }

        [TestMethod]
        public void TestMethod1()
        {
            //var repository = new EfNoTrackEmployeeRepository("LabDbContext");
            //var repository = new Linq2EmployeeRepository("LabDbContext");

            //var repository = new DapperEmployeeRepository("LabDbContext");
            //var repository = new DataReaderEmployeeRepository("LabDbContext");

            //var employeesFromDb = repository.GetAllEmployeesDetail(out var count);

            //Assert.IsTrue(employeesFromDb.Count() > 0);
            IEnumerable<Tuple<int, string, string>> authors =
                new[]
                {
                    Tuple.Create(1, "Isaac", "Asimov"),
                    Tuple.Create(2, "Robert", "Heinlein"),
                    Tuple.Create(3, "Frank", "Herbert"),
                    Tuple.Create(4, "Aldous", "Huxley")
                };

            Console.WriteLine(authors.ToStringTable(
                                                    new[] { "Id", "First Name", "Surname" },
                                                    a => a.Item1, a => a.Item2, a => a.Item3));
        }

        [TestMethod]
        public void Benchmark()
        {
            this.Run(Reports, this._time);
        }

        [TestMethod]
        public void JoinBenchmark()
        {
            this.Run(JoinReports, this._time);
        }

        private void Run(IEnumerable<TestReport> reports, int runTime)
        {
            var totalReports = new List<TestReport>();
            var detailRows = new List<Tuple<int>>();
            foreach (var report in reports)
            {
                report.Run(runTime);

                var totalReport = new TestReport { Name = report.Name };
                foreach (var testReport in report.TestReports)
                {
                    totalReport.CostTime += testReport.CostTime;
                    totalReport.DataCount += testReport.DataCount;
                    detailRows.Add(Tuple.Create(testReport.Index));
                }

                totalReports.Add(totalReport);
            }

            var totalRows = totalReports.OrderBy(p => p.CostTime)
                                        .Select((p, i) => Tuple.Create(i + 1, p.Name, p.CostTime, p.DataCount))
                                        .ToList();

            var totalTable = totalRows.ToStringTable(new[] { "Fastest", "Name", "CostTime", "DataCount" },
                                                     a => a.Item1,
                                                     a => a.Item2,
                                                     a => a.Item3,
                                                     a => a.Item4
                                                    );
            var detialTable = detailRows.ToStringTable(new[] { "Index" },
                                         a => a.Item1
                                        );
            Console.WriteLine(totalTable);
            Console.WriteLine(detialTable);

            //for (var i = 0; i < sortTestReports.Count; i++)
            //{
            //    var report = sortTestReports[i];
            //    totalTestInfos.Add(Tuple.Create(i + 1,
            //                                    report.Name,
            //                                    0,
            //                                    report.CostTime,
            //                                    report.DataCount));
            //}
            //var aa = totalTestInfos.ToStringTable(new[] { "Id", "Name", "Index", "Cost Time", "Data Count" },
            //                                          a => a.Item1, a => a.Item2, a => a.Item3, a => a.Item4, a => a.Item5);
            //Console.WriteLine(aa);
            //var sortTestReports = reports.OrderBy(p => p.TotalCostTime).ToList();
            //var totalTestInfos = new List<Tuple<int, string, int, double, long>>();

            //for (var i = 0; i < sortTestReports.Count; i++)
            //{
            //    var report = sortTestReports[i];
            //    totalTestInfos.Add(Tuple.Create(i + 1, report.Name, 0, report.TotalCostTime,
            //                                    report.TotalDataCount));
            //}

            //var table = totalTestInfos.ToStringTable(new[] {"Fastest", "Name", "Times", "Cost Time", "Data Count"},
            //                                         a => a.Item1, a => a.Item2, a => a.Item3, a => a.Item4,
            //                                         a => a.Item5);

            //Console.WriteLine(table);

            //for (var i = 0; i < sortTestReports.Count; i++)
            //{
            //    var report = sortTestReports[i];

            //    //Console.WriteLine($"第 {i + 1} 名，{report.TestInfo}");
            //    Console.WriteLine(report.TestInfo);
            //}
        }

        private static IEnumerable<TestReport> CreateTestReports()
        {
            var reports = new HashSet<TestReport>();
            foreach (var repository in Utility.Repositories)
            {
                var testReport = new TestReport(repository.Key + ".GetAllEmployees",
                                                () =>
                                                {
                                                    var value = repository.Value;
                                                    value.GetAllEmployees(out var count);
                                                    return new DataInfo { RowCount = count };
                                                });
                reports.Add(testReport);
            }

            foreach (var repository in Utility.AdoRepositories)
            {
                var testReport = new TestReport(repository.Key + ".GetAllEmployees",
                                                () =>
                                                {
                                                    var value = repository.Value;
                                                    value.GetAllEmployees(out var count);
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
                var testReport = new TestReport(repository.Key + ".GetAllEmployeesDetail",
                                                () =>
                                                {
                                                    var value = repository.Value;
                                                    value.GetAllEmployeesDetail(out var count);
                                                    return new DataInfo { RowCount = count };
                                                });
                reports.Add(testReport);
            }

            foreach (var repository in Utility.AdoRepositories)
            {
                var testReport = new TestReport(repository.Key + ".GetAllEmployeesDetail",
                                                () =>
                                                {
                                                    var value = repository.Value;
                                                    value.GetAllEmployeesDetail(out var count);
                                                    return new DataInfo { RowCount = count };
                                                });
                reports.Add(testReport);
            }

            return reports;
        }
    }
}