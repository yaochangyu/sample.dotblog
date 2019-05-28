namespace Lab.EF6.DynamicDataMask.UnitTest.EntityModel
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

        public virtual DbSet<Customer> Customers { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Customer>()
                .Property(e => e.ID)
                .IsUnicode(false);

            modelBuilder.Entity<Customer>()
                .Property(e => e.Marriage)
                .IsFixedLength()
                .IsUnicode(false);

            modelBuilder.Entity<Customer>()
                .Property(e => e.Email)
                .IsUnicode(false);

            modelBuilder.Entity<Customer>()
                .Property(e => e.Tel)
                .IsUnicode(false);

            modelBuilder.Entity<Customer>()
                .Property(e => e.Salary)
                .HasPrecision(13, 2);

            modelBuilder.Entity<Customer>()
                .Property(e => e.CreditCard)
                .IsUnicode(false);
        }
    }
}
