using System;
using System.Data.Entity.SqlServer;
using System.Diagnostics.Contracts;
using Lab.Db.TestCase.DAL;
using TechTalk.SpecFlow;

namespace Lab.Db.TestCase.UnitTest
{
    [Binding]
    internal class TestHook
    {
        private const string HOST_ADDRESS = "http://localhost:9527";

        public static readonly string   TestData   = "出發吧，跟我一起進入偉大的航道";
        public static readonly DateTime TestNow    = new DateTime(1900, 1, 1, 0, 0, 0);
        public static readonly string   TestUserId = "TEST_USER";

        [AfterTestRun]
        public static void AfterTestRun()
        {
            using (var dbContext = new LabDbContext(會員管理作業V1Steps.ConnectionString))
            {
                if (dbContext.Database.Exists())
                {
                    dbContext.Database.Delete();
                }
            }
        }

        [BeforeTestRun]
        public static void BeforeTestRun()
        {
            var instance = SqlProviderServices.Instance;
            using (var dbContext = new LabDbContext(會員管理作業V1Steps.ConnectionString))
            {
                if (dbContext.Database.Exists())
                {
                    dbContext.Database.Delete();
                }

                dbContext.Database.Initialize(true);
            }
        }

        public static int DeleteAll(string connectionString)
        {
            var sql = @"
-- disable referential integrity
EXEC sp_MSForEachTable 'ALTER TABLE ? NOCHECK CONSTRAINT ALL' 


EXEC sp_MSForEachTable 'DELETE FROM ?' 


-- enable referential integrity again 
EXEC sp_MSForEachTable 'ALTER TABLE ? WITH CHECK CHECK CONSTRAINT ALL' 
";
            using (var dbContext = new LabDbContext(connectionString))
            {
                return dbContext.Database.ExecuteSqlCommand(sql);
            }
        }

        public static Guid Parse(string id)
        {
            Contract.Requires(!string.IsNullOrWhiteSpace(id), "!string.IsNullOrWhiteSpace(id)");
            var _guidFormat = "{0}-0000-0000-0000-000000000000";
            var guidText    = string.Format(_guidFormat, id.PadRight(8, '0'));
            var key         = Guid.Parse(guidText);
            return key;
        }
    }
}