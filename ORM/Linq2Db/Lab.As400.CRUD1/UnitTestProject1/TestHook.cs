using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LinqToDB;
using LinqToDB.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UnitTestProject1.EntityModel;

namespace UnitTestProject1
{
    [TestClass]
    public class TestHook
    {
        public static readonly string TestData = "出發吧，跟我一起進入偉大的航道";

        public static void Delete()
        {
            using (var db = new MemberDb())
            {
                db.BeginTransaction();
                try
                {
                    db.Members.Where(p => p.REMARK == TestData).Delete();
                    db.Identities.Where(p => p.REMARK == TestData).Delete();

                    db.CommitTransaction();
                }
                catch (Exception e)
                {
                    Console.WriteLine(db.LastQuery);
                    Console.WriteLine(e);
                    db.RollbackTransaction();
                    throw;
                }
            }
        }
    }
}