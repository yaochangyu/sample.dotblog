using System.Data.Entity;
using Lab.HangfireServer.Migrations;

namespace Lab.HangfireServer.DAL.EntityModel
{
    public class DemoDbContext : DbContext
    {
        private static bool[] s_migrated=new []{false};

        public DbSet<Member> Members { get; set; }

        public DemoDbContext()
            : base("name=Demo")
        {
        }

        public DemoDbContext(string connectionStringName)
            : base(connectionStringName)
        {
        }

        public static DemoDbContext Create(string connectionStringName = "Demo")
        {
            var dbContext = new DemoDbContext(connectionStringName);
            dbContext.Configuration.AutoDetectChangesEnabled = false;
            dbContext.Configuration.ProxyCreationEnabled     = false;
            dbContext.Configuration.LazyLoadingEnabled       = false;
            return dbContext;
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            Migrate();
            modelBuilder.Entity<Member>().HasKey(r => r.Id, config => config.IsClustered(false) );

        }
        private static void Migrate()
        {
            if (!s_migrated[0])
            {
                lock (s_migrated)
                {
                    if (!s_migrated[0])
                    {
                        Database.SetInitializer(new MigrateDatabaseToLatestVersion<DemoDbContext, Configuration>());
                        s_migrated[0] = true;
                    }
                }
            }
        }
    }
}