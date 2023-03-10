using Microsoft.EntityFrameworkCore;

namespace Lab.StepDependencyInjection.WebAPI.EntityModel
{
    public class EmployeeDbContext : DbContext
    {
        public virtual DbSet<Employee> Employees { get; set; }

        public EmployeeDbContext(DbContextOptions<EmployeeDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Employee>(p =>
            {
                p.HasKey(e => e.Id);
                p.HasIndex(e => e.SequenceId)
                    .IsUnique();
                p.Property(p => p.Remark).IsRequired(false);
            });
        }
    }
}