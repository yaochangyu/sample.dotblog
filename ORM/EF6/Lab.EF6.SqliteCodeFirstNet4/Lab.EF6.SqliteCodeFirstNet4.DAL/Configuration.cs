using System.Data.Entity.Migrations;
using System.Data.SQLite.EF6.Migrations;

namespace Lab.EF6.SqliteCodeFirstNet4.DAL
{
    public class Configuration : DbMigrationsConfiguration<LabDbContext>
    {
        public Configuration()
        {
            this.AutomaticMigrationsEnabled        = true;
            this.AutomaticMigrationDataLossAllowed = true;
            this.SetSqlGenerator("System.Data.SQLite", new SQLiteMigrationSqlGenerator());
        }

        protected override void Seed(LabDbContext context)
        {
            //This method will be called after migrating to the latest version.

            //You can use the DbSet<T>.AddOrUpdate() helper extension method
            //to avoid creating duplicate seed data.E.g.

            //context.Employees
            //       .AddOrUpdate(p => p.Name,
            //                    new Employee {Name = "Andrew Peters"},
            //                    new Employee {Name = "Brice Lambson"},
            //                    new Employee {Name = "Rowan Miller"}
            //                   );
        }
    }
}