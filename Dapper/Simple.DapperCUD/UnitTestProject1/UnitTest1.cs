using System;
using Dapper.Contrib.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UnitTestProject1.EntityModel;

namespace UnitTestProject1
{
    [TestClass]
    public class UnitTest1
    {
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
    }
}