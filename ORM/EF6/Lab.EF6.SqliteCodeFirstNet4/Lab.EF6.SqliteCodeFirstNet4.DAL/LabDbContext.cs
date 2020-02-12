using System.Data.Entity;

namespace Lab.EF6.SqliteCodeFirstNet4.DAL
{
    public class LabDbContext : DbContext
    {
        private static readonly bool[] s_migrated = {false};

        public DbSet<Employee> Employees { get; set; }

        public LabDbContext() : base("DefaultConnection")
        {
            Migrate();
        }

        private static void Migrate()
        {
            if (!s_migrated[0])
            {
                lock (s_migrated)
                {
                    if (!s_migrated[0])
                    {
                        Database.SetInitializer(new MigrateDatabaseToLatestVersion<LabDbContext,
                                                    Configuration>());
                        s_migrated[0] = true;
                    }
                }
            }
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
        }
    }
}