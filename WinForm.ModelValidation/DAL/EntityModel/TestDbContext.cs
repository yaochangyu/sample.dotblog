using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.EntityModel
{
    public class TestDbContext : DbContext
    {
        public TestDbContext() : base("TestDbContext")
        {
        }

        public DbSet<Member> Members { get; set; }
        public DbSet<MemberLog> MemberLogs { get; set; }
    }
}
