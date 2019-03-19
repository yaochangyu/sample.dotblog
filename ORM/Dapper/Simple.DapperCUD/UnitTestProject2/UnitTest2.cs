using System;
using System.Linq;
using LinqToDB;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UnitTestProject2.EntityModel;

namespace UnitTestProject2
{
    [TestClass]
    public class UnitTest2
    {
        [TestCleanup]
        public void TestCleanup()
        {
            TestHook.DeleteAll();
        }

        [TestInitialize]
        public void TestInitialize()
        {
            TestHook.DeleteAll();
        }

        [TestMethod]
        public void FirstOrDefault_Test1()
        {
            var member = InsertTestRecord();
            var expected = "小章";
            var updateName = "yao";

            using (var db = new MemberDb())
            {
                db.Members
                  .Select(p => new Member() {Id = p.Id, Name = p.Name})
                  .FirstOrDefault();
            }
        }

        [TestMethod]
        public void Where_Test1()
        {
            var member = InsertTestRecord();
            var expected = "小章";
            var updateName = "yao";

            using (var db = new MemberDb())
            {
                db.Members
                  .Select(p => new Member() {Id = p.Id, Name = p.Name})
                  .Where(p=>p.Id==Guid.Parse(member.Id.ToString()))
                  .FirstOrDefault();
            }
        }
        [TestMethod]
        public void Update_Test1()
        {
            var member = InsertTestRecord();
            var expected = "小章";
            var updateName = "yao";

            using (var db = new MemberDb())
            {
                db.Members
                  .Select(p => new Member() { Id = p.Id, Name = p.Name })
                  .Where(p => p.Id == member.Id)
                  .Set(p => p.Name, updateName)
                  .Update();
            }
        }
        [TestMethod]
        public void Update_Test2()
        {
            var member = InsertTestRecord();
            var expected = "小章";
            var updateName = "yao";

            using (var db = new MemberDb())
            {
            }
        }

        [TestMethod]
        public void Insert_Test1()
        {
            using (var dbConnection = new MemberDb())
            {
                var memberToDb = new Member
                {
                    Id = Guid.NewGuid(),
                    Name = "Yao",
                    Age = 12
                };
                var count = dbConnection.Insert(memberToDb);
                Assert.IsTrue(count > 0);
            }
        }
        private static Member InsertTestRecord()
        {
            using (var dbConnection = new MemberDb())
            {
                var memberToDb = new Member
                {
                    Id = Guid.NewGuid(),
                    Name = "Yao",
                    Age = 12
                };
                var count = dbConnection.Insert(memberToDb);
                return memberToDb;
            }
        }
    }
}
