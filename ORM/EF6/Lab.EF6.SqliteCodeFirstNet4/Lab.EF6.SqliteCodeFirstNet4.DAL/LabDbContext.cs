using System.Data.Entity;
using SQLite.CodeFirst;

namespace Lab.EF6.SqliteCodeFirstNet4.DAL
{
    public class LabDbContext : DbContext
    {
        private static readonly bool[] s_migrated = {false};

        public DbSet<Employee> Employees { get; set; }

        public LabDbContext() : base("DefaultConnection")
        {
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            if (!s_migrated[0])
            {
                lock (s_migrated)
                {
                    if (!s_migrated[0])
                    {
                        var initializer = new SqliteDropCreateDatabaseWhenModelChanges<LabDbContext>(modelBuilder);
                        Database.SetInitializer(initializer);

                        s_migrated[0] = true;
                    }
                }
            }
        }
    }
}