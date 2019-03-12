using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTestProject2
{
    /// <summary>
    ///     Summary description for UnitTest1
    /// </summary>
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            BenchmarkManager.Add("ef", () =>
                                        {
                                            var employees = Utility
                                                            .Repositories[RepositoryNames.EfNoTrackEmployeeRepository]
                                                            .GetAllEmployees(out var count);
                                            return new DataInfo {RowCount = count};
                                        });
            BenchmarkManager.Add("dapper", () =>
                                      {
                                          var employees = Utility
                                                          .Repositories[RepositoryNames.DapperEmployeeRepository]
                                                          .GetAllEmployees(out var count);
                                          return new DataInfo {RowCount = count};
                                      });
            BenchmarkManager.Statistics(4);
        }

    }
}