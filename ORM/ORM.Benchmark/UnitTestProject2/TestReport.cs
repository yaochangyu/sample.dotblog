using System;
using System.Diagnostics;
using System.Text;

namespace UnitTestProject2
{
    public class TestReport
    {
        private readonly Func<DataInfo> action;
        private readonly string testName;

        public TestReport(string testName, Func<DataInfo> action)
        {
            this.action = action;
            this.testName = testName;
        }

        public string TestInfo { get; set; }

        public double TotalCostTime { get; set; }

        public long TotalDataCount { get; set; }

        public string TotalTestInfo { get; set; }

        public void Run(int runTimes)
        {
            var repository = this.action;
            var name = this.testName;

            var reportBuilder = new StringBuilder();

            reportBuilder.AppendFormat("執行 {0} 測試", name);
            reportBuilder.AppendLine();
            var index = 0;

            long totalDataCount = 0;
            double totalCostTime = 0;
            while (runTimes-- > 0)
            {
                index++;

                int dataCount;
                var watch = Stopwatch.StartNew();
                var dataInfo = repository.Invoke();

                dataCount = dataInfo.RowCount;
                watch.Stop();

                var costTime = watch.Elapsed.TotalMilliseconds;
                reportBuilder.AppendFormat("\t");
                reportBuilder.AppendFormat($"第 {index} 執行結果, " +
                                           $"共花費:{costTime} ms, " +
                                           $"取得 {dataCount} 筆");
                reportBuilder.AppendLine();

                totalDataCount += dataCount;
                totalCostTime += costTime;
            }

            this.TotalTestInfo = $"執行 {name} 測試, " +
                                 $"共花費:{totalCostTime} ms, " +
                                 $"共執行:{index} 次，取得筆數：{totalDataCount}";

            this.TestInfo = reportBuilder.ToString();
            this.TotalCostTime = totalCostTime;
            this.TotalDataCount = totalDataCount;
        }
    }
}