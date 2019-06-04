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
        public static string TempConnectionString =
            @"data source=(localdb)\mssqllocaldb;initial catalog=Lab.Db.TestCase.UnitTest.{0};integrated security=True;MultipleActiveResultSets=True;App=EntityFramework";

        private const string HOST_ADDRESS = "http://localhost:9527";

        public static readonly string   TestData   = "出發吧，跟我一起進入偉大的航道";
        public static readonly DateTime TestNow    = new DateTime(1900, 1, 1, 0, 0, 0);
        public static readonly string   TestUserId = "TEST_USER";

        [AfterTestRun]
        public static void AfterTestRun()
        {
            DeleteDb(會員管理作業V1Steps.ConnectionString);
            DeleteDb(會員管理作業V2Steps.ConnectionString);
            DeleteDb(會員管理作業V3Steps.ConnectionString);
            DeleteDb(會員管理作業V4Steps.ConnectionString);
        }

        [BeforeTestRun]
        public static void BeforeTestRun()
        {
            var instance = SqlProviderServices.Instance;
            InitialDb(會員管理作業V1Steps.ConnectionString);
            InitialDb(會員管理作業V2Steps.ConnectionString);
            InitialDb(會員管理作業V3Steps.ConnectionString);
            InitialDb(會員管理作業V4Steps.ConnectionString);
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

        private static void DeleteDb(string connectionString)
        {
            using (var dbContext = new LabDbContext(connectionString))
            {
                if (dbContext.Database.Exists())
                {
                    dbContext.Database.Delete();
                }
            }
        }

        private static void InitialDb(string connectionString)
        {
            using (var dbContext = new LabDbContext(connectionString))
            {
                if (dbContext.Database.Exists())
                {
                    dbContext.Database.Delete();
                }

                dbContext.Database.Initialize(true);
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