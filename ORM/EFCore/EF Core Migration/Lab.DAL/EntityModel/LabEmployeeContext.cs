using Microsoft.EntityFrameworkCore;

namespace Lab.DAL.EntityModel
{
    public class LabEmployeeContext : DbContext
    {
        private static readonly bool[] s_migrated = {false};

        public virtual DbSet<Employee> Employees { get; set; }

        public virtual DbSet<Identity> Identities { get; set; }

        public virtual DbSet<Order> Orders { get; set; }

        public LabEmployeeContext()
        {
            
        }

        public LabEmployeeContext(DbContextOptions<LabEmployeeContext> options)
            : base(options)
        {
            if (options == null)
            {
                options = DbOptionsFactory.DbContextOptions;
            }

            if (!s_migrated[0])
            {
                lock (s_migrated)
                {
                    if (!s_migrated[0])
                    {
                        this.Database.Migrate();
                        s_migrated[0] = true;
                    }
                }
            }
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            //var connectionString = "Server=(localdb)\\mssqllocaldb;Database=LabEmployee.DAL;Trusted_Connection=True;";
            var connectionString = DbOptionsFactory.ConnectionString;
            if (!optionsBuilder.IsConfigured)
            {
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. See http://go.microsoft.com/fwlink/?LinkId=723263 for guidance on storing connection strings.
                optionsBuilder
                    .UseSqlServer(connectionString);
            }
        }
    }
}