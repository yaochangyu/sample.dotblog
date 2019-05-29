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

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Employee>()
                .Property(e => e.Bonus)
                .HasPrecision(3, 1);
        }
    }
}
