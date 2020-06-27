using Microsoft.EntityFrameworkCore;

namespace EFCore3.EntityModel
{
    public class LabDbContext : DbContext
    {
        private static readonly bool[] s_migrated = {false};

        public virtual DbSet<Member> Members { get; set; }

        public LabDbContext()
        {
        }

        public LabDbContext(DbContextOptions<LabDbContext> options)
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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Member>(p =>
                                        {
                                            p.HasKey(e => e.Id)
                                             .HasName("PK_Member")
                                             .IsClustered(false);
                                            p.HasIndex(e => e.SequenceId)
                                             .HasName("CLIX_Member_SequenceId")
                                             .IsUnique()
                                             .IsClustered();
                                        });
        }

        //        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        //        {
        //            //var connectionString = "Server=(localdb)\\mssqllocaldb;Database=LabEmployee.DAL;Trusted_Connection=True;";
        //            var connectionString = DbOptionsFactory.ConnectionString;
        //            if (!optionsBuilder.IsConfigured)
        //            {
        //#warning To protect potentially sensitive information in your connection string, you should move it out of source code. See http://go.microsoft.com/fwlink/?LinkId=723263 for guidance on storing connection strings.
        //                optionsBuilder
        //                    .UseSqlServer(connectionString);
        //            }
        //        }
    }
}