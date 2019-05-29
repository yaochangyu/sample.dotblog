using System.Data;
using System.Data.Entity;

namespace Lab.EF6.DynamicDataMask.UnitTest.EntityModel
{
    public partial class TestDbContext : DbContext
    {
        public TestDbContext(string connectionStringName = "TestDbContext") : base(connectionStringName)
        {
        }

        public static TestDbContext CreateDbContext(string connectionStringName = "TestDbContext")
        {
            var dbContext = new TestDbContext(connectionStringName);
            if (dbContext.Database.Connection.State == ConnectionState.Closed)
            {
                dbContext.Database.Connection.Open();
            }

            return dbContext;
        }
    }
}