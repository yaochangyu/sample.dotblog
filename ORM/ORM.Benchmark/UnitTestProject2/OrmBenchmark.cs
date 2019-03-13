using BenchmarkDotNet.Attributes;

namespace UnitTestProject2
{
    public class OrmBenchmark
    {
        [Benchmark]
        public void AdapterEmployeeRepository()
        {
            Utility.Repositories[RepositoryNames.AdapterEmployeeRepository].GetAllEmployees(out var count);
        }

        [Benchmark]
        public void DapperEmployeeRepository()
        {
            Utility.Repositories[RepositoryNames.DapperEmployeeRepository].GetAllEmployees(out var count);
        }

        [Benchmark]
        public void DataReaderEmployeeRepository()
        {
            Utility.Repositories[RepositoryNames.DataReaderToTableEmployeeRepository].GetAllEmployees(out _);
        }

        [Benchmark]
        public void EfNoTrackEmployeeRepository()
        {
            Utility.Repositories[RepositoryNames.EfNoTrackEmployeeRepository].GetAllEmployees(out var count);
        }

        [Benchmark]
        public void Linq2EmployeeRepository()
        {
            Utility.Repositories[RepositoryNames.Linq2EmployeeRepository].GetAllEmployees(out var count);
        }

        [Benchmark]
        public void LoadDataEmployeeRepository()
        {
            Utility.Repositories[RepositoryNames.LoadDataEmployeeRepository].GetAllEmployees(out var count);
        }

        [Benchmark]
        public void LoadEmployeeRepository()
        {
            Utility.Repositories[RepositoryNames.LoadEmployeeRepository].GetAllEmployees(out var count);
        }
    }
}