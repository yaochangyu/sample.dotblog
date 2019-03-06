using System;
using Dapper;
using Dapper.Contrib.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UnitTestProject1.EntityModel;

namespace UnitTestProject1
{
    [TestClass]
    public class UnitTest1
    {
        [TestInitialize]
        public void TestInitialize()
        {
            TestHook.DeleteAll();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            TestHook.DeleteAll();
        }

        [TestMethod]
        public void Insert_Test()
        {
            using (var dbConnection = DbManager.CreateConnection())
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

        [TestMethod]
        public void Update_Test()
        {
            var member = Insert();
            var expected = "小章";
            using (var dbConnection = DbManager.CreateConnection())
            {
                member.Name = expected;
                var status = dbConnection.Update(member);
                var memberFromDb = dbConnection.QueryFirstOrDefault<Member>("select * from Member");
                Assert.AreEqual(expected, memberFromDb.Name);
            }
        }

        private static Member Insert()
        {
            using (var dbConnection = DbManager.CreateConnection())
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