using BenchmarkDotNet.Attributes;

namespace UnitTestProject2
{
    public class OrmBenchmark
    {
        [Benchmark]
        public void AdapterEmployeeRepository()
        {
            int count;
            Utility.Repositories[RepositoryNames.AdapterEmployeeRepository].GetAllEmployees(out count);
        }

        [Benchmark]
        public void DapperEmployeeRepository()
        {
            int count;
            Utility.Repositories[RepositoryNames.DapperEmployeeRepository].GetAllEmployees(out count);
        }

        [Benchmark]
        public void DataReaderEmployeeRepository()
        {
            int count;
            Utility.Repositories[RepositoryNames.DataReaderToTableEmployeeRepository].GetAllEmployees(out count);
        }

        [Benchmark]
        public void EfNoTrackEmployeeRepository()
        {
            int count;
            Utility.Repositories[RepositoryNames.EfNoTrackEmployeeRepository].GetAllEmployees(out count);
        }

        [Benchmark]
        public void Linq2EmployeeRepository()
        {
            int count;
            Utility.Repositories[RepositoryNames.Linq2EmployeeRepository].GetAllEmployees(out count);
        }

        [Benchmark]
        public void LoadDataEmployeeRepository()
        {
            int count;
            Utility.Repositories[RepositoryNames.LoadDataEmployeeRepository].GetAllEmployees(out count);
        }

        [Benchmark]
        public void LoadEmployeeRepository()
        {
            int count;
            Utility.Repositories[RepositoryNames.LoadEmployeeRepository].GetAllEmployees(out count);
        }
    }
}