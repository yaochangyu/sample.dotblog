using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core.Mapping;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnitTestProject2.Repository;
using UnitTestProject2.Repository.Ado;
using UnitTestProject2.Repository.Dapper;
using UnitTestProject2.Repository.Ef;
using UnitTestProject2.Repository.Ef.EntityModel;
using UnitTestProject2.Repository.Linq2Db;

namespace UnitTestProject2
{
    class Utility
    {
        public static Dictionary<RepositoryNames, IEmployeeRepository> Repositories;
        public static Dictionary<RepositoryNames, IAdoEmployeeRepository> AdoRepositories;

        static Utility()
        {
            string connectionName = "LabDbContext";
            if (Utility.Repositories == null)
            {
                Utility.Repositories = InitialRepositories(connectionName);
            }

            if (Utility.AdoRepositories == null)
            {
                Utility.AdoRepositories = InitialAdoRepositories(connectionName);
            }

            Database.SetInitializer<LabDbContext>(null);
            using (var dbcontext = new LabDbContext(connectionName))
            {
                var objectContext = ((IObjectContextAdapter)dbcontext).ObjectContext;
                var mappingCollection =
                    (StorageMappingItemCollection)objectContext.MetadataWorkspace.GetItemCollection(DataSpace.CSSpace);
                mappingCollection.GenerateViews(new List<EdmSchemaError>());
            }

            //Run(Reports,1);
            foreach (var repository in Utility.Repositories)
            {
                int count;
                repository.Value.GetAllEmployees(out count);
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
                {RepositoryNames.DataReaderEmployeeRepository, new DataReaderEmployeeRepository(connectionName)},
                {RepositoryNames.LoadDataEmployeeRepository, new LoadDataEmployeeRepository(connectionName)},
                {RepositoryNames.LoadEmployeeRepository, new LoadEmployeeRepository(connectionName)},
                {RepositoryNames.AdapterEmployeeRepository, new AdapterEmployeeRepository(connectionName)}
            };

            return actions;
        }
    }
}
