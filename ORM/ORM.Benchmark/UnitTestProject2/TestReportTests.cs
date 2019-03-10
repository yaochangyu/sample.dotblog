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
                                                    new[] {"Id", "First Name", "Surname"},
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
            var detailReports = new Dictionary<string, List<Tuple<int, string, int, double, long>>>();
            foreach (var report in reports)
            {
                report.Run(runTime);

                var totalReport = new TestReport {Name = report.Name};
                foreach (var testReport in report.TestReports)
                {
                    totalReport.CostTime += testReport.CostTime;
                    totalReport.DataCount += testReport.DataCount;
                }

                totalReport.TestReports = report.TestReports;
                totalReports.Add(totalReport);
            }

            var sortReports = totalReports.OrderBy(p => p.CostTime)
                                          .ToList();
            var totalRows = sortReports.Select((p, i) => Tuple.Create(i + 1,
                                                                      p.Name,
                                                                      p.CostTime,
                                                                      p.DataCount))
                                       .ToList();

            for (int i = 0; i < sortReports.Count; i++)
            {
                var sortReport = sortReports[i];
                foreach (var testReport in sortReport.TestReports)
                {
                    var tuple = new Tuple<int, string, int, double, long>(i + 1,
                                                                          testReport.Name,
                                                                          testReport.Index,
                                                                          testReport.CostTime,
                                                                          testReport.DataCount);
                    if (detailReports.ContainsKey(sortReport.Name))
                    {
                        detailReports[sortReport.Name].Add(tuple);
                    }
                    else
                    {
                        detailReports.Add(sortReport.Name, new List<Tuple<int, string, int, double, long>> {tuple});
                    }
                }
            }

            var totalTable = totalRows.ToStringTable(new[] {"Fastest", "Name", "CostTime", "DataCount"},
                                                     a => a.Item1,
                                                     a => a.Item2,
                                                     a => a.Item3,
                                                     a => a.Item4
                                                    );
            Console.WriteLine(totalTable);

            foreach (var detailReport in detailReports)
            {
                var detailTable = detailReport.Value
                                              .ToStringTable(new[] {"Fastest", "Name", "Index", "CostTime", "DataCount"},
                                                             a => a.Item1, a => a.Item2, a => a.Item3, a => a.Item4,
                                                             a => a.Item5
                                                            );
                Console.WriteLine(detailTable);
            }
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
                                                    return new DataInfo {RowCount = count};
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
                                                    return new DataInfo {RowCount = count};
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
                                                    return new DataInfo {RowCount = count};
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
                                                    return new DataInfo {RowCount = count};
                                                });
                reports.Add(testReport);
            }

            return reports;
        }
    }
}