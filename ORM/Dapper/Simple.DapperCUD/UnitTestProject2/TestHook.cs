using System;
using System.Data.Entity;
using System.Data.Entity.SqlServer;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UnitTestProject2.EntityModel;

namespace UnitTestProject2
{
    [TestClass]
    public class TestHook
    {
        [AssemblyInitialize]
        public static void AssemblyInitialize(TestContext context)
        {
            var instance = SqlProviderServices.Instance;
            Database.SetInitializer(new DropCreateDatabaseAlways<TestDbContext>());

            //Database.SetInitializer(new TestDropCreateDatabaseAlways());

            using (var dbContext = new TestDbContext())
            {
                if (dbContext.Database.Exists())
                {
                    dbContext.Database.Delete();
                }

                dbContext.Database.Initialize(true);
            }
        }
        public static string DeleteAll()
        {
            var sql = @"
-- disable referential integrity
EXEC sp_MSForEachTable 'ALTER TABLE ? NOCHECK CONSTRAINT ALL' 


EXEC sp_MSForEachTable 'DELETE FROM ?' 


-- enable referential integrity again 
EXEC sp_MSForEachTable 'ALTER TABLE ? WITH CHECK CHECK CONSTRAINT ALL' 
";
            using (var dbContext = new TestDbContext())
            {
                dbContext.Database.ExecuteSqlCommand(sql);
            }

            return sql;
        }
    }
}