namespace EF6.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class InitialCreate : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Member",
                c => new
                    {
                        Id = c.Guid(nullable: false),
                        Name = c.String(nullable: false, maxLength: 200),
                        Age = c.Int(nullable: false),
                        SequenceId = c.Long(nullable: false, identity: true),
                    })
                .PrimaryKey(t => t.Id, clustered: false)
                .Index(t => t.SequenceId, clustered: true, name: "CLIX_Member_SequenceId");
            
        }
        
        public override void Down()
        {
            DropIndex("dbo.Member", "CLIX_Member_SequenceId");
            DropTable("dbo.Member");
        }
    }
}
