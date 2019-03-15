using System;
using System.Collections.Generic;
using System.Linq;
using Faker;
using LinqToDB;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UnitTestProject1.EntityModel;

namespace UnitTestProject1
{
    [TestClass]
    public class UnitTest1
    {
        [TestInitialize]
        public void Before()
        {
            TestHook.Delete();
        }

        [TestCleanup]
        public void After()
        {
            TestHook.Delete();
        }

        [TestMethod]
        public void 新增()
        {
            var memberToDb = new Member
            {
                ID = 1,
                NAME = "yao",
                AGE = 20,
                REMARK = TestHook.TestData
            };
            using (var db = new MemberDb())
            {
                var insertCount = db.Insert(memberToDb);
                Console.WriteLine(db.LastQuery);
                Assert.AreEqual(1, insertCount);
            }
        }

        [TestMethod]
        public void 修改部分()
        {
            var fromDb = this.Insert();
            var name = "yao2";
            using (var db = new MemberDb())
            {
                var updateCount = db.Members
                                    .Where(p => p.ID == fromDb.ID)
                                    .Set(p => p.NAME, name)
                                    .Update();
                Console.WriteLine(db.LastQuery);
                Assert.AreEqual(1, updateCount);
            }
        }

        [TestMethod]
        public void 修改一整筆()
        {
            var fromDb = this.Insert();
            var updateDb = new Member
            {
                ID = fromDb.ID,
                NAME = "yao1",
                REMARK = TestHook.TestData
            };
            using (var db = new MemberDb())
            {
                var updateCount = db.Update(updateDb);
                Console.WriteLine(db.LastQuery);
                Assert.AreEqual(1, updateCount);
            }
        }

        [TestMethod]
        public void 查詢IDENTITY_InnerJoin()
        {
            this.Inserts();

            using (var db = new MemberDb())
            {
                var filter = db.Identities
                               .Select(p => new
                               {
                                   p.MEMBER.ID,
                                   p.MEMBER.AGE,
                                   p.MEMBER.NAME,
                                   p.PASSWORD,
                                   p.ACCOUNT
                               })
                               .Where(p => p.ID > 0)
                               .ToList()
                    ;
                Console.WriteLine(db.LastQuery);
            }
        }

        [TestMethod]
        public void 查詢MEMBER_OuterJoin()
        {
            this.Inserts();

            using (var db = new MemberDb())
            {
                var filter = db.Members
                               .Select(p => new
                               {
                                   p.ID,
                                   p.AGE,
                                   p.NAME,
                                   p.IDENTITY.PASSWORD,
                                   p.IDENTITY.ACCOUNT
                               })
                               .Where(p => p.ID > 0)
                               .ToList()
                    ;
                Console.WriteLine(db.LastQuery);
            }
        }

        [TestMethod]
        public void 交易()
        {
            this.Inserts();
            var memberToDb = new Member
            {
                ID = 1,
                NAME = "yao",
                AGE = 20,
                REMARK = TestHook.TestData
            };
            using (var db = new MemberDb())
            {
                db.BeginTransaction();
                try
                {
                    db.Insert(memberToDb);
                    throw new Exception();

                    db.CommitTransaction();
                }
                catch (Exception e)
                {
                    db.RollbackTransaction();
                }
            }
        }

        private Member Insert()
        {
            var memberToDb = new Member
            {
                ID = 12,
                NAME = "yao",
                AGE = 13,
                REMARK = TestHook.TestData
            };

            using (var db = new MemberDb())
            {
                db.Insert(memberToDb);
            }

            return memberToDb;
        }

        private void Inserts()
        {
            var membersToDb = new List<Member>();
            var identitiesToDb = new List<Identity>();
            for (int i = 0; i < 10; i++)
            {
                membersToDb.Add(new Member
                {
                    ID = i + 1,
                    NAME = Name.FullName(),
                    AGE = RandomNumber.Next(1, 120),
                    REMARK = TestHook.TestData
                });
                identitiesToDb.Add(new Identity
                {
                    MEMBER_ID = membersToDb[i].ID,
                    ACCOUNT = Name.First(),
                    PASSWORD = "12345",
                    REMARK = TestHook.TestData
                });
            }

            using (var db = new MemberDb())
            {
                db.BeginTransaction();
                try
                {
                    //db.BulkCopy(membersToDb);
                    for (int i = 0; i < membersToDb.Count; i++)
                    {
                        var member = membersToDb[i];
                        var identity = identitiesToDb[i];
                        db.Insert(member);
                        db.Insert(identity);
                    }

                    db.CommitTransaction();
                }
                catch (Exception e)
                {
                    Console.WriteLine(db.LastQuery);
                    db.RollbackTransaction();
                }
            }
        }
    }
}