using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
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
        private static readonly string    ConnectionName = "SLabDbContext";
        internal                Validator Validator      = new Validator("yaochang");

        [TestMethod]
        public void InnerJoin查詢()
        {
            using (var db = new LabEmployee2DB(ConnectionName))
            {
                var selector = db.Identities
                                 .Where(p => p.SequenceId > 0)
                                 .Select(p => new EmployeeViewModel
                                 {
                                     Id         = p.Employee.Id,
                                     Name       = p.Employee.Name,
                                     Age        = p.Employee.Age,
                                     SequenceId = p.Employee.SequenceId,

                                     Account  = p.Account,
                                     Password = p.Password
                                 });
                var count  = selector.Count();
                var result = selector.OrderBy(p => p.SequenceId).ToList();
                Assert.IsTrue(result.Count > 1);
            }
        }

        [TestMethod]
        public void 交易()
        {
            using (var db = new LabEmployee2DB(ConnectionName))
            {
                db.BeginTransaction();
                var count = 0;
                try
                {
                    var employee = new Employee {Id         = Guid.NewGuid(), Name = "小章", Age       = 18};
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

        [TestMethod]
        public void 更新一筆()
        {
            var toDb = Insert();
            using (var db = new LabEmployee2DB(ConnectionName))
            {
                var updateDb = new Employee
                {
                    Id   = toDb.Id,
                    Name = "小章",
                    Age  = 19
                };
                var count = db.Update(updateDb);
                Assert.IsTrue(count == 1);
            }
        }

        [TestMethod]
        public void 呼叫預存()
        {
            using (var db = new LabEmployee2DB(ConnectionName))
            {
                var count = db.InsertOrUpdateEmployee(Guid.NewGuid(), "yao", 19, "Remark");
                Assert.IsTrue(count == 1);
            }
        }

        [TestMethod]
        public void 呼叫預存2()
        {
            using (var db = new LabEmployee2DB(ConnectionName))
            {
                var toDb = new DataTable();
                toDb.Columns.Add("Id",     typeof(Guid));
                toDb.Columns.Add("Name",   typeof(string));
                toDb.Columns.Add("Age",    typeof(int));
                toDb.Columns.Add("Remark", typeof(string));

                var row = toDb.NewRow();
                row["Id"]     = Guid.NewGuid();
                row["Name"]   = "yao";
                row["Age"]    = 19;
                row["Remark"] = "Remark";
                toDb.Rows.Add(row);
                var count = db.InsertOrUpdateEmployee2(toDb);
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
                var count      = updateable.Update();

                Assert.IsTrue(count == 1);
            }
        }

        [TestMethod]
        public void 新增一筆()
        {
            //using (var db = LabEmployee2DB.CreateSecretDb())
            //{
            //    if (db.Connection.State == ConnectionState.Closed)
            //    {
            //        db.Connection.Open();
            //    }

            //    var count = db.Insert(new Employee {Id = Guid.NewGuid(), Name = "小章", Age = 18});
            //    Assert.IsTrue(count == 1);

            //    db.Connection.Close();
            //}

            using (var db = LabEmployee2DB.CreateSecretDb())
            {
                var count = db.Insert(new Employee {Id = Guid.NewGuid(), Name = "小章", Age = 18});
                Assert.IsTrue(count == 1);
            }
        }

        [TestMethod]
        public void 新增大量資料()
        {
            var employees = new List<Employee>();
            for (var i = 0; i < 10000; i++)
            {
                employees.Add(new Employee
                {
                    Id   = Guid.NewGuid(),
                    Name = Name.FullName(),
                    Age  = RandomNumber.Next(1, 120)
                });
            }

            using (var db = new LabEmployee2DB(ConnectionName))
            {
                var bulkCopyOptions = new BulkCopyOptions
                    {BulkCopyType = BulkCopyType.ProviderSpecific, UseInternalTransaction = true};
                db.BulkCopy(bulkCopyOptions, employees);
            }
        }

        [TestMethod]
        public void 新增多筆()
        {
            using (var db = LabEmployee2DB.CreateSecretDb())
            {
                var employee1 = new Employee {Id = Guid.NewGuid(), Name = "余小章", Age = 20};
                var employee2 = new Employee {Id = Guid.NewGuid(), Name = "小章", Age  = 18};
                var employees = new List<Employee> {employee2, employee1};
                var count     = db.Insert(employees, "Employee");
                Assert.IsTrue(count == 2);
            }
        }

        private void Insert(LabEmployee2DB db = null)
        {
            var isDispose = false;
            if (db == null)
            {
                db        = new LabEmployee2DB(ConnectionName);
                isDispose = true;
            }

            try
            {
                //db.Insert()...
            }
            finally
            {
                if (isDispose)
                {
                    if (db.Connection.State == ConnectionState.Open)
                    {
                        db.Connection.Close();
                    }
                }
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

    public class Validator
    {
        private readonly DESCryptoServiceProvider des    = new DESCryptoServiceProvider();
        private readonly byte[]                   rgbIv  = new byte[8];
        private readonly byte[]                   rgbKey = new byte[8];

        public Validator(string key)
        {
            var hash = MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(key));
            Array.Copy(hash, 0, this.rgbKey, 0, 8);
            Array.Copy(hash, 8, this.rgbIv,  0, 8);
        }

        public string Decrypt(string encryptText)
        {
            using (var ms = new MemoryStream(Convert.FromBase64String(encryptText)))
            {
                using (var cs = new CryptoStream(ms, this.des.CreateDecryptor(this.rgbKey, this.rgbIv),
                                                 CryptoStreamMode.Read))
                {
                    using (var sr = new StreamReader(cs))
                    {
                        return sr.ReadToEnd();
                    }
                }
            }
        }

        public string Encrypt(string rawText)
        {
            using (var ms = new MemoryStream())
            {
                using (var cs = new CryptoStream(ms, this.des.CreateEncryptor(this.rgbKey, this.rgbIv),
                                                 CryptoStreamMode.Write))
                {
                    var buff = Encoding.UTF8.GetBytes(rawText);
                    cs.Write(buff, 0, buff.Length);
                }

                return Convert.ToBase64String(ms.ToArray());
            }
        }
    }
}