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
        private static readonly IEnumerable<string> _tableNames = new List<string>
        {
            "MEMBER",
            "IDENTITY"
        };

        public static readonly string TestData = "出發吧，跟我一起進入偉大的航道";

        public static void Delete()
        {
            var deleteCommand = GetDeleteCommand(TestData, "REMARK", _tableNames.ToArray());
            using (var db = new MemberDb())
            {
                db.BeginTransaction();
                try
                {
                    db.Members.Where(p => p.REMARK == TestData).Delete();
                    db.Identities.Where(p => p.REMARK == TestData).Delete();

                    //var count = db.Execute(deleteCommand);
                    db.CommitTransaction();
                }
                catch (Exception e)
                {
                    Console.WriteLine(db.LastQuery);
                    Console.WriteLine(e);
                    db.RollbackTransaction();
                }
            }
        }

        public static string GetDeleteCommand(string testData,
                                              string columnName = "Remark",
                                              params string[] tableNames)
        {
            var commandBuilder = new StringBuilder();
            foreach (var tableName in tableNames)
            {
                if (string.IsNullOrWhiteSpace(tableName))
                {
                    continue;
                }

                var deleteCommand = $@"delete from {tableName}
where {columnName} = '{testData}';
";

                commandBuilder.AppendLine(deleteCommand);
            }

            commandBuilder.AppendLine("commit;");
            return commandBuilder.ToString();
        }
    }
}