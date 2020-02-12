using System.Data.Entity;

namespace Lab.EF6.SqliteCodeFirst.UnitTest.EntityModel
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
            //modelBuilder.Entity<Employee>().HasKey(r => r.Id, config => config.IsClustered(true) );

            //modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();
            //modelBuilder.Configurations.AddFromAssembly(typeof(EmployeeDbContext).Assembly);
        }
    }
}