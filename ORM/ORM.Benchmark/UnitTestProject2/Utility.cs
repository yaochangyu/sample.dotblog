using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core.Mapping;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Infrastructure;
using UnitTestProject2.Repository;
using UnitTestProject2.Repository.Ado;
using UnitTestProject2.Repository.Dapper;
using UnitTestProject2.Repository.Ef;
using UnitTestProject2.Repository.Ef.EntityModel;
using UnitTestProject2.Repository.Linq2Db;

namespace UnitTestProject2
{
    internal class Utility
    {
        public static Dictionary<RepositoryNames, IEmployeeRepository> Repositories;
        public static Dictionary<RepositoryNames, IAdoEmployeeRepository> AdoRepositories;

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

            //不檢查migration table
            Database.SetInitializer<LabDbContext>(null);

            //載入對應
            using (var dbcontext = new LabDbContext(connectionName))
            {
                var objectContext = ((IObjectContextAdapter) dbcontext).ObjectContext;
                var mappingCollection =
                    (StorageMappingItemCollection) objectContext.MetadataWorkspace.GetItemCollection(DataSpace.CSSpace);
                mappingCollection.GenerateViews(new List<EdmSchemaError>());
            }

            //暖機
            //切換連線字串
            foreach (var repository in Repositories)
            {
                repository.Value.GetAllEmployees(out var count);
                repository.Value.GetAllEmployeesDetail(out count);
                //repository.Value.ConnectionName = "LabDbContextLarge";
            }

            foreach (var repository in AdoRepositories)
            {
                repository.Value.GetAllEmployees(out var count);
                repository.Value.GetAllEmployeesDetail(out count);
                //repository.Value.ConnectionName = "LabDbContextLarge";
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
                {RepositoryNames.DataReaderEmployeeRepository, new DataReaderToTableEmployeeRepository(connectionName)},
                {RepositoryNames.LoadDataEmployeeRepository, new LoadDataEmployeeRepository(connectionName)},
                {RepositoryNames.LoadEmployeeRepository, new LoadEmployeeRepository(connectionName)},
                {RepositoryNames.AdapterEmployeeRepository, new AdapterEmployeeRepository(connectionName)}
            };

            return actions;
        }
    }
}