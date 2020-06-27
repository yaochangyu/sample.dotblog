using Microsoft.EntityFrameworkCore;

namespace EFCore3.EntityModel
{
    public class LabContext : DbContext
    {
        private static readonly bool[] s_migrated = {false};

        public virtual DbSet<Member> Members { get; set; }


        public LabContext()
        {

        }

        public LabContext(DbContextOptions<LabContext> options)
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