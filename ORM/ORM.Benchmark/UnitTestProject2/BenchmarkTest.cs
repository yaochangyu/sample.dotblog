using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core.Mapping;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UnitTestProject2.Repository.Ef.EntityModel;

namespace UnitTestProject2
{
    /// <summary>
    ///     Summary description for UnitTest1
    /// </summary>
    [TestClass]
    public class BenchmarkTest
    {
        static BenchmarkTest()
        {
            string connectionName = "LabDbContext";
            foreach (var repository in Utility.AdoRepositories)
            {
                BenchmarkManager.Add(repository.Key.ToString(),
                                     () =>
                                     {
                                         var employees = repository.Value.GetAllEmployees(out var count);
                                         return new DataInfo {RowCount = count};
                                     });
            }

            foreach (var repository in Utility.Repositories)
            {
                BenchmarkManager.Add(repository.Key.ToString(),
                                     () =>
                                     {
                                         var employees = repository.Value.GetAllEmployees(out var count);
                                         return new DataInfo {RowCount = count};
                                     });
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
            BenchmarkManager.Warm();
            Utility.Warm();
            //Utility.SwitchLargeDb();
        }

        [TestMethod]
        public void Benchmark_Statistics_10()
        {
            BenchmarkManager.Statistics(10);
        }
    }
}