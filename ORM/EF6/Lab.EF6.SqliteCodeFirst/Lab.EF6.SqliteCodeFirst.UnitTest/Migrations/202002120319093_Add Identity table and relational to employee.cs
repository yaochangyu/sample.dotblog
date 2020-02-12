namespace Lab.EF6.SqliteCodeFirst.UnitTest.EntityModel
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddIdentitytableandrelationaltoemployee : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Identity",
                c => new
                    {
                        Employee_Id = c.Guid(nullable: false),
                        Account = c.String(maxLength: 2147483647),
                        Password = c.String(maxLength: 2147483647),
                    })
                .PrimaryKey(t => t.Employee_Id)
                .ForeignKey("dbo.Employee", t => t.Employee_Id)
                .Index(t => t.Employee_Id)
                .Index(t => t.Account, unique: true, name: "UK_Account");
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Identity", "Employee_Id", "dbo.Employee");
            DropIndex("dbo.Identity", "UK_Account");
            DropIndex("dbo.Identity", new[] { "Employee_Id" });
            DropTable("dbo.Identity");
        }
    }
}
