using System.Data.Entity;

namespace Lab.NoMagicNumeric.EntityModel.DAL
{
    public class LabDbContext : DbContext
    {
        public DbSet<Order> Orders { get; set; }
    }
}