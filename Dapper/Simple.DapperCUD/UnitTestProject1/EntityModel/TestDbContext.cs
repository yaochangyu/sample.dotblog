using System.Data.Entity;

namespace UnitTestProject1.EntityModel
{
    internal class TestDbContext : DbContext
    {
        public DbSet<Member> Members { get; set; }
    }
}