using System.Linq;
using System.Threading;
using Lab.Db.TestCase.DAL;
using Lab.Db.TestCase.Infrastructure;
using TechTalk.SpecFlow;
using TechTalk.SpecFlow.Assist;

namespace Lab.Db.TestCase.UnitTest
{

    [Binding]
    [Scope(Feature = "會員管理作業V3")]
    public class 會員管理作業V3Steps : Steps
    {
        public static string ConnectionString = null;

        static 會員管理作業V3Steps()
        {
            ConnectionString = string.Format(TestHook.TempConnectionString, "會員管理作業V3");
        }

        [After]
        public void After()
        {
            TestHook.DeleteAll(ConnectionString);
        }

        [Before]
        public void Before()
        {
            TestHook.DeleteAll(ConnectionString);
        }

        [Given(@"前端傳來以下DeleteMemberRequest")]
        public void Given前端傳來以下DeleteMemberRequest(Table table)
        {
            var fromUI = table.CreateInstance<DeleteMemberRequest>();
            this.ScenarioContext.Set(fromUI, "fromUI");
        }

        [Given(@"前端傳來以下InsertMemberRequest")]
        public void Given前端傳來以下InsertMemberRequest(Table table)
        {
            var fromUI = table.CreateInstance(() => new InsertMemberRequest());
            this.ScenarioContext.Set(fromUI, "fromUI");
        }

        [Given(@"前端傳來以下UpdateMemberRequest")]
        public void Given前端傳來以下UpdateMemberRequest(Table table)
        {
            var fromUI = table.CreateInstance(() => new UpdateMemberRequest());
            this.ScenarioContext.Set(fromUI, "fromUI");
        }

        [Given(@"資料庫的Member資料表已存在以下資料")]
        public void Given資料庫的Member資料表已存在以下資料(Table table)
        {
            var toDb = table.CreateSet(() => new Member
            {
                Remark = TestHook.TestData,
                CreateAt = TestHook.TestNow,
                CreateBy = TestHook.TestUserId
            });
            using (var dbContext = LabDbContext.Create(ConnectionString))
            {
                dbContext.Members.AddRange(toDb);
                dbContext.SaveChanges();
            }
        }

        [Then(@"預期資料庫的Member資料表有以下資料")]
        public void Then預期資料庫的Member資料表有以下資料(Table table)
        {
            using (var dbContext = LabDbContext.Create(ConnectionString))
            {
                var members = dbContext.Members.AsNoTracking().ToList();
                table.CompareToSet(members);
            }
        }

        [When(@"調用MemberRepository\.Delete")]
        public void When調用MemberRepository_Delete()
        {
            var fromUI = this.ScenarioContext.Get<DeleteMemberRequest>("fromUI");
            var repository = new MemberRepository();
            repository.ConnectionString = ConnectionString;
            repository.Delete(fromUI, TestHook.TestUserId);
        }

        [When(@"調用MemberRepository\.Insert")]
        public void When調用MemberRepository_Insert()
        {
            var fromUI = this.ScenarioContext.Get<InsertMemberRequest>("fromUI");
            var repository = new MemberRepository();
            repository.Now = TestHook.TestNow;
            repository.Id = TestHook.Parse("1");
            repository.ConnectionString = ConnectionString;
            repository.Insert(fromUI, TestHook.TestUserId);
        }

        [When(@"調用MemberRepository\.Update")]
        public void When調用MemberRepository_Update()
        {
            var fromUI = this.ScenarioContext.Get<UpdateMemberRequest>("fromUI");
            var repository = new MemberRepository();
            repository.Now = TestHook.TestNow;
            repository.ConnectionString = ConnectionString;
            repository.Update(fromUI, TestHook.TestUserId);
        }
    }
}