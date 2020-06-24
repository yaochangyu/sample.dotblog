using System.Data.Entity;

namespace EF6.EntityModel
{
    internal class LabDbContext : DbContext
    {
        public DbSet<Member> Members { get; set; }

        public LabDbContext()
        {
            Migrate();

        }
        public LabDbContext(string connectionStringName)
            : base("LabDbContext")
        {
            Migrate();
        }

        public static LabDbContext Create(string connectionStringName = "LabDbContext")
        {
            var dbContext = new LabDbContext(connectionStringName);
            dbContext.Configuration.LazyLoadingEnabled       = false;
            dbContext.Configuration.ProxyCreationEnabled     = false;
            dbContext.Configuration.AutoDetectChangesEnabled = false;
            return dbContext;
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Member>()
                        .HasKey(r => r.Id,
                                config => config.IsClustered(false) );

        }
        private static bool[] s_migrated =new []{false};

        private static void Migrate()
        {
            if (!s_migrated[0])
            {
                lock (s_migrated)
                {
                    if (!s_migrated[0])
                    {
                        //Database.SetInitializer(new MigrateDatabaseToLatestVersion<LabDbContext, Configuration>());
                        s_migrated[0] = true;
                    }
                }
            }
        }

    }
}