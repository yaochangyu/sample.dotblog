using System;
using System.Collections.Generic;
using System.Linq;
using Faker;
using Lab.EntityModel;
using LinqToDB;
using LinqToDB.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lab.UnitTest
{
    [TestClass]
    public class UnitTest1
    {
        private static readonly string ConnectionName = "LabDbContext";

        [TestMethod]
        public void InnerJoin查詢()
        {
            using (var db = new LabEmployee2DB(ConnectionName))
            {
                var selector = db.Identities
                                 .Where(p => p.SequenceId > 0)
                                 .Select(p => new EmployeeViewModel
                                 {
                                     Id = p.Employee.Id,
                                     Name = p.Employee.Name,
                                     Age = p.Employee.Age,
                                     SequenceId = p.Employee.SequenceId,

                                     Account = p.Account,
                                     Password = p.Password
                                 });
                var count = selector.Count();
                var result = selector.OrderBy(p => p.SequenceId).ToList();
                Assert.IsTrue(result.Count > 1);
            }
        }

        [TestMethod]
        public void 更新一筆()
        {
            var toDb = Insert();
            using (var db = new LabEmployee2DB(ConnectionName))
            {
                var updateDb = new Employee
                {
                    Id = toDb.Id,
                    Name = "小章",
                    Age = 19
                };
                var count = db.Update(updateDb);
                Assert.IsTrue(count == 1);
            }
        }

        [TestMethod]
        public void 部分更新()
        {
            var toDb = Insert();
            using (var db = new LabEmployee2DB(ConnectionName))
            {
                var selector = db.Employees
                                 .Where(p => p.Id == toDb.Id)
                    ;
                var updateable = selector.Set(p => p.Name, "yao");
                var count = updateable.Update();

                Assert.IsTrue(count == 1);
            }
        }

        [TestMethod]
        public void 新增一筆()
        {
            using (var db = new LabEmployee2DB(ConnectionName))
            {
                var count = db.Insert(new Employee {Id = Guid.NewGuid(), Name = "小章", Age = 18});
                Assert.IsTrue(count == 1);
            }
        }

        [TestMethod]
        public void 新增大量資料()
        {
            List<Employee> employees = new List<Employee>();
            for (int i = 0; i < 10000; i++)
            {
                employees.Add(new Employee
                {
                    Id = Guid.NewGuid(),
                    Name = Name.FullName(),
                    Age = RandomNumber.Next(1, 120)
                });
            }

            using (var db = new LabEmployee2DB(ConnectionName))
            {
                db.BulkCopy(employees);
            }
        }

        [TestMethod]
        public void 交易()
        {
            using (var db = new LabEmployee2DB(ConnectionName))
            {
                db.BeginTransaction();
                int count = 0;
                try
                {
                    var employee = new Employee {Id = Guid.NewGuid(), Name = "小章", Age = 18};
                    var identity = new Identity {EmployeeId = employee.Id, Account = "yao", Password = "123456"};
                    count += db.Insert(employee);
                    count += db.Insert(identity);
                    db.CommitTransaction();
                }
                catch (Exception e)
                {
                    db.RollbackTransaction();
                }

                Assert.IsTrue(count == 2);
            }
        }

        private static Employee Insert()
        {
            var toDb = new Employee {Id = Guid.NewGuid(), Name = "小章", Age = 18};
            using (var db = new LabEmployee2DB(ConnectionName))
            {
                db.Insert(toDb);
            }

            return toDb;
        }
    }
}