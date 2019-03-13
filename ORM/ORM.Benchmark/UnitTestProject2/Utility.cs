using System.Collections.Generic;
using UnitTestProject2.Repository;
using UnitTestProject2.Repository.Ado;
using UnitTestProject2.Repository.Dapper;
using UnitTestProject2.Repository.Ef;
using UnitTestProject2.Repository.Linq2Db;

namespace UnitTestProject2
{
    internal class Utility
    {
        public static Dictionary<RepositoryNames, IEmployeeRepository> Repositories;
        public static Dictionary<RepositoryNames, IAdoEmployeeRepository> AdoRepositories;
        private static bool s_isWarm = false;

        static Utility()
        {
            string connectionName = "LabDbContext";
            if (Repositories == null)
            {
                Repositories = InitialRepositories(connectionName);
            }

            if (AdoRepositories == null)
            {
                AdoRepositories = InitialAdoRepositories(connectionName);
            }
        }

        private static Dictionary<RepositoryNames, IEmployeeRepository> InitialRepositories(string connectionName)
        {
            var actions = new Dictionary<RepositoryNames, IEmployeeRepository>
            {
                {RepositoryNames.EfNoTrackEmployeeRepository, new EfNoTrackEmployeeRepository(connectionName)},
                {RepositoryNames.Linq2EmployeeRepository, new Linq2EmployeeRepository(connectionName)},
                {RepositoryNames.DapperEmployeeRepository, new DapperEmployeeRepository(connectionName)}
            };

            return actions;
        }

        private static Dictionary<RepositoryNames, IAdoEmployeeRepository> InitialAdoRepositories(string connectionName)
        {
            var actions = new Dictionary<RepositoryNames, IAdoEmployeeRepository>
            {
                {RepositoryNames.DataReaderToTableEmployeeRepository, new DataReaderToTableEmployeeRepository(connectionName)},
                {RepositoryNames.LoadDataEmployeeRepository, new LoadDataEmployeeRepository(connectionName)},
                {RepositoryNames.LoadEmployeeRepository, new LoadEmployeeRepository(connectionName)},
                {RepositoryNames.AdapterEmployeeRepository, new AdapterEmployeeRepository(connectionName)}
            };

            return actions;
        }

        public static void Warm()
        {
            foreach (var repository in Repositories)
            {
                repository.Value.GetAllEmployees(out var count1);
                repository.Value.GetAllEmployeesDetail(out var count2);
            }

            foreach (var repository in AdoRepositories)
            {
                repository.Value.GetAllEmployees(out var count1);
                repository.Value.GetAllEmployeesDetail(out var count2);
            }
        }

        public static void SwitchLargeDb()
        {
            //切換連線字串
            foreach (var repository in Repositories)
            {
                repository.Value.ConnectionName = "LabDbContextLarge";
            }

            foreach (var repository in AdoRepositories)
            {
                repository.Value.ConnectionName = "LabDbContextLarge";
            }
        }
    }
}