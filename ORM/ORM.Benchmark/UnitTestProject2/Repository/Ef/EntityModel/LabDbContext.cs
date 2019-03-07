using System.Data.Entity;

namespace UnitTestProject2.Repository.Ef.EntityModel
{
    public class LabDbContext : DbContext
    {
        public LabDbContext(string connectionName) : base(connectionName)
        {
        }

        public virtual DbSet<Employee> Employees { get; set; }

        public virtual DbSet<Identity> Identities { get; set; }

        public virtual DbSet<Order> Orders { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            //modelBuilder.Entity<Employee>()
            //            .HasMany(e => e.Identities)
            //            .WithOptional(e => e.Employee)
            //            .HasForeignKey(e => e.Employee_Id);
        }
    }
}