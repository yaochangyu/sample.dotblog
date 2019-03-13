using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace UnitTestProject2
{
    internal class BenchmarkManager
    {
        private readonly Dictionary<string, Func<Report>> _targets;

        public BenchmarkManager()
        {
            if (_targets == null)
            {
                _targets = new Dictionary<string, Func<Report>>();
            }
        }

        public List<Report> Statistics(int count = 1)
        {
            var firstReports = new Dictionary<string, List<Report>>();
            var currentCount = count;
            var watch = new Stopwatch();
            foreach (var target in this._targets)
            {
                var index = 1;
                while (count-- > 0)
                {
                    watch.Restart();

                    var report = target.Value.Invoke();

                    watch.Stop();

                    report.CostTime = watch.Elapsed.TotalMilliseconds;
                    report.Name = target.Key;
                    report.Index = index;
                    if (firstReports.ContainsKey(target.Key) == false)
                    {
                        firstReports.Add(target.Key, new List<Report> { report });
                    }
                    else
                    {
                        firstReports[target.Key].Add(report);
                    }

                    index++;
                }

                count = currentCount;
            }

            var totalReports = GenerateTotalReports(firstReports);
            var detailReports = GenerateDetailReport(totalReports);
            return totalReports;
        }

        public void Warm()
        {
            foreach (var target in _targets)
            {
                target.Value.Invoke();
            }
        }

        private static List<Report> GenerateTotalReports(Dictionary<string, List<Report>> sourceInfos)
        {
            var totalReports = new List<Report>();

            foreach (var sourceInfo in sourceInfos)
            {
                var totalReport = new Report();
                var details = new List<Report>();
                var runCount = 0;
                foreach (var info in sourceInfo.Value)
                {
                    totalReport.CostTime += info.CostTime;
                    totalReport.RowCount += info.RowCount;
                    details.Add(info);
                    runCount++;
                }

                totalReport.Name = sourceInfo.Key;
                totalReport.Average = totalReport.CostTime / runCount;
                totalReport.RunCount = runCount;
                totalReport.Details = details;
                totalReports.Add(totalReport);
            }

            var sortReports = totalReports.OrderBy(p => p.CostTime)
                                          .ToList();
            var totalRows = sortReports.Select((p, i) => Tuple.Create(i + 1,
                                                                      p.Name,
                                                                      p.CostTime,
                                                                      p.Average.ToString("#0.0000"),
                                                                      p.RunCount,
                                                                      p.RowCount))
                                       .ToList();
            var totalTable =
                totalRows.ToStringTable(new[] { "Fastest", "Name", "CostTime (ms)", "Average (ms)", "RunCount", "DataCount" },
                                        a => a.Item1,
                                        a => a.Item2,
                                        a => a.Item3,
                                        a => a.Item4,
                                        a => a.Item5,
                                        a => a.Item6
                                       );
            Console.WriteLine(totalTable);
            return sortReports;
        }

        private static Dictionary<string, List<Tuple<int, string, int, double, long>>> GenerateDetailReport(
            List<Report> sortReports)
        {
            var detailReports = new Dictionary<string, List<Tuple<int, string, int, double, long>>>();

            for (int i = 0; i < sortReports.Count; i++)
            {
                var sortReport = sortReports[i];
                foreach (var testReport in sortReport.Details)
                {
                    var tuple = new Tuple<int, string, int, double, long>(i + 1,
                                                                          testReport.Name,
                                                                          testReport.Index,
                                                                          testReport.CostTime,
                                                                          testReport.RowCount);
                    if (detailReports.ContainsKey(sortReport.Name))
                    {
                        detailReports[sortReport.Name].Add(tuple);
                    }
                    else
                    {
                        detailReports.Add(sortReport.Name, new List<Tuple<int, string, int, double, long>> { tuple });
                    }
                }
            }

            foreach (var detailReport in detailReports)
            {
                var detailTable = detailReport.Value
                                              .ToStringTable(new[] { "Fastest", "Name", "Index", "CostTime (ms)", "DataCount" },
                                                             a => a.Item1, a => a.Item2, a => a.Item3, a => a.Item4,
                                                             a => a.Item5
                                                            );
                Console.WriteLine(detailTable);
            }

            return detailReports;
        }

        public void Add(string name, Func<Report> target)
        {
            if (this._targets.ContainsKey(name))
            {
                this._targets[name] = target;
            }
            else
            {
                this._targets.Add(name, target);
            }
        }
    }
}