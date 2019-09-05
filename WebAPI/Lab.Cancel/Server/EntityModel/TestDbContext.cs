using System.Data.Entity;

namespace Server.EntityModel
{
    public class TestDbContext : DbContext
    {
        public DbSet<Product> Products { get; set; }

        public TestDbContext() : base("name=TestDbContext")
        {
        }

        public static TestDbContext Create()
        {
            var dbContext = new TestDbContext();
            dbContext.Configuration.AutoDetectChangesEnabled = false;
            dbContext.Configuration.LazyLoadingEnabled       = false;
            dbContext.Configuration.ProxyCreationEnabled     = false;
            return dbContext;
        }
    }
}