namespace Lab.EF6.AlwaysEncrypt.UnitTest.EntityModel
{
    using System;
    using System.Data.Entity;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Linq;

    public partial class TestDbContext : DbContext
    {
        public TestDbContext()
            : base("name=TestDbContext")
        {
        }

        public virtual DbSet<Employee> Employees { get; set; }
        public virtual DbSet<Identity> Identities { get; set; }
        public virtual DbSet<Order> Orders { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Employee>()
                .Property(e => e.Bonus)
                .HasPrecision(3, 1);

            modelBuilder.Entity<Employee>()
                .HasOptional(e => e.Identity)
                .WithRequired(e => e.Employee);

            modelBuilder.Entity<Employee>()
                .HasMany(e => e.Orders)
                .WithOptional(e => e.Employee)
                .HasForeignKey(e => e.Employee_Id);

            modelBuilder.Entity<Order>()
                .Property(e => e.Price)
                .HasPrecision(4, 2);
        }
    }
}
