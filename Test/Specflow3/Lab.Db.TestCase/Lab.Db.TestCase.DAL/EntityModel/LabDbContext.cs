using System.Configuration;
using System.Data;
using System.Data.Entity;

namespace Lab.Db.TestCase.DAL
{
    public class LabDbContext : DbContext
    {
        public DbSet<Member> Members { get; set; }

        public LabDbContext() : base("LabDbContext")
        {
        }

        public LabDbContext(string connectionStringName) : base(connectionStringName)
        {
        }

        public static LabDbContext Create(string connectionStringName = "MemberDbContext")
        {
            var          setting   = ConfigurationManager.ConnectionStrings[connectionStringName];
            LabDbContext dbContext = null;
            if (setting != null)
            {
                //TODO:連線字串解密
                var connectString = setting.ConnectionString;
                dbContext = new LabDbContext(connectString);
            }
            else
            {
                dbContext = new LabDbContext(connectionStringName);
            }

            dbContext.Configuration.AutoDetectChangesEnabled = false;
            dbContext.Configuration.LazyLoadingEnabled       = false;
            dbContext.Configuration.ProxyCreationEnabled     = false;

            if (dbContext.Database.Exists())
            {
                if (dbContext.Database.Connection.State == ConnectionState.Closed)
                {
                    dbContext.Database.Connection.Open();
                }
            }

            return dbContext;
        }

        protected override void Dispose(bool disposing)
        {
            if (this.Database.Connection.State == ConnectionState.Open)
            {
                this.Database.Connection.Close();
            }

            base.Dispose(disposing);
        }
    }
}