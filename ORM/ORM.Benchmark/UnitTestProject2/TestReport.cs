using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace UnitTestProject2
{
    public class TestReport
    {
        private readonly Func<DataInfo> action;

        //private readonly string testName;
        public TestReport()
        {
        }

        public TestReport(string testName, Func<DataInfo> action)
        {
            this.action = action;
            this.Name = testName;
        }

        public double CostTime { get; set; }

        public long DataCount { get; set; }

        public string Name { get; set; }

        public int Index { get; set; }

        public IEnumerable<TestReport> TestReports { get; set; }

        public void Run(int runTimes)
        {
            var repository = this.action;
            var name = this.Name;
            var index = 0;

            var reports = new List<TestReport>();
            while (runTimes-- > 0)
            {
                index++;
                var info = new TestReport {Name = name, Index = index};
                var watch = Stopwatch.StartNew();

                var dataInfo = repository.Invoke();

                watch.Stop();
                info.DataCount = dataInfo.RowCount;
                info.CostTime = watch.Elapsed.TotalMilliseconds;

                reports.Add(info);
            }

            this.TestReports = reports;
            ////this.TestInfo = testInfos.ToStringTable(new[] {"Name", "Index", "Cost Time", "Data Count"},
            ////                                        a => a.Item1, a => a.Item2, a => a.Item3, a => a.Item4);
            //this.TotalCostTime = totalCostTime;
            //this.TotalDataCount = totalDataCount;
        }
    }
}