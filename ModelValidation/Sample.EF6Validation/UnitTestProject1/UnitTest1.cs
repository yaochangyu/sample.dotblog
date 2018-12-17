using System;
using System.Data.Entity.Validation;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UnitTestProject1.EntityModel;

namespace UnitTestProject1
{
    [TestClass]
    public class UnitTest1
    {
        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            using (var dbContext = new ValidationDbContext())
            {
                if (dbContext.Database.Exists())
                {
                    dbContext.Database.Delete();
                }

                dbContext.Database.Initialize(true);
            }
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            using (var dbContext = new ValidationDbContext())
            {
                if (dbContext.Database.Exists())
                {
                    dbContext.Database.Delete();
                }
            }
        }
        [TestMethod]
        public void 新增Member時_若MemberLogs不存在_調用DbContext_Save_應預期得到例外()
        {
            using (var dbContext = new ValidationDbContext())
            {
                var id = Guid.NewGuid();
                var memberToDb = new Member
                {
                    Id = id
                };


                dbContext.Members
                         .Add(memberToDb);

                Action action = () => { dbContext.SaveChanges(); };
                action.Should().Throw<DbEntityValidationException>();
            }
        }
        [TestMethod]
        public void 欄位有DateTimeMin_調用DbContext_Save_應預期得到例外()
        {
            using (var dbContext = new ValidationDbContext())
            {
                var id = Guid.NewGuid();
                var memberToDb = new Member
                {
                    Id = id
                };

                memberToDb.Logs
                          .Add(new MemberLog {Id = Guid.NewGuid()});

                dbContext.Members
                         .Add(memberToDb);
                Action action = () => { dbContext.SaveChanges(); };
                action.Should().Throw<DbEntityValidationException>();
            }
        }
    }
}