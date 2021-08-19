using LinqToDB;
using LinqToDB.Data;

namespace UnitTestProject1.EntityModel
{
    public class MemberDb : DataConnection
    {
        public MemberDb()
            : base("MemberDb")
        {
        }

        //public ITable<MJVNTR> MJVNTRs { get { return this.GetTable<MJVNTR>(); } }
        public ITable<Member> Members
        {
            get { return this.GetTable<Member>(); }
        }

        public ITable<Identity> Identities
        {
            get { return this.GetTable<Identity>(); }
        }
    }
}