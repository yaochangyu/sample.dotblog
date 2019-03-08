using LinqToDB;
using UnitTestProject2.EntityModel;

namespace UnitTestProject2
{
    public class MemberDb : LinqToDB.Data.DataConnection
    {
        public MemberDb() : base("TestDbContext")
        {
            this.Members = this.GetTable<Member>();
        }

        public ITable<Member> Members { get; set; }

        // ... other tables ...
    }
}