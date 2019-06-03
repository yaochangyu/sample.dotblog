using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using FluentAssertions;
using Lab.EF6.AlwaysEncrypt.UnitTest.EntityModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lab.EF6.AlwaysEncrypt.UnitTest
{
    [TestClass]
    public class EF6_Solution
    {
        [ClassCleanup]
        public static void ClassCleanup()
        {
            DeleteAll();
        }

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            DeleteAll();
            Initial2();
        }

        private static void DeleteAll()
        {
            var sql = @"
-- disable referential integrity
EXEC sp_MSForEachTable 'ALTER TABLE ? NOCHECK CONSTRAINT ALL' 

EXEC sp_MSForEachTable 'DELETE FROM ?' 

-- enable referential integrity again 
EXEC sp_MSForEachTable 'ALTER TABLE ? WITH CHECK CHECK CONSTRAINT ALL' 
";
            using (var dbContext = new TestDbContext())
            {
                dbContext.Database.ExecuteSqlCommand(sql);
            }
        }

        private static void Initial()
        {
            using (var dbContext = new TestDbContext())
            {
                var employee1 = new Employee
                {
                    Id       = Guid.NewGuid(),
                    Name     = "小章",
                    Age      = 18,
                    CreateAt = new DateTime(2019, 12, 1),
                    Identity = new Identity {Account = "yao", Password = "123456"}
                };
                employee1.Orders = new List<Order>
                {
                    new Order
                    {
                        Id          = Guid.NewGuid(),
                        Employee    = employee1,
                        Employee_Id = employee1.Id,
                        Price       = (decimal) 20.0, ProductName = "滑鼠"
                    }
                };
                var employeesToDb = new List<Employee>
                {
                    employee1,
                    new Employee
                    {
                        Id       = Guid.NewGuid(),
                        Name     = "小英",
                        Age      = 23,
                        CreateAt = new DateTime(1909, 1, 2),
                        Identity = new Identity {Account = "James", Password = "123456"}
                    },
                    new Employee
                    {
                        Id       = Guid.NewGuid(),
                        Name     = "小明",
                        Age      = 33,
                        CreateAt = new DateTime(2011, 2, 2),
                        Identity = new Identity {Account = "JOJO", Password = "123456"}
                    }
                };
                dbContext.Employees.AddRange(employeesToDb);
                dbContext.SaveChanges();
            }
        }

        private static void Initial2()
        {
            using (var dbContext = new TestDbContext())
            {
                dbContext.Configuration.AutoDetectChangesEnabled = false;
                var employee1 = new Employee
                {
                    Id       = Guid.NewGuid(),
                    Name     = "小章",
                    Age      = 18,
                    CreateAt = new DateTime(2019, 12, 1),
                    Identity = new Identity {Account = "yao", Password = "123456"}
                };
                employee1.Orders = new List<Order>
                {
                    new Order
                    {
                        Id          = Guid.NewGuid(),
                        Employee    = employee1,
                        Employee_Id = employee1.Id,
                        Price       = (decimal) 20.0, ProductName = "滑鼠"
                    },
                    new Order
                    {
                        Id          = Guid.NewGuid(),
                        Employee    = employee1,
                        Employee_Id = employee1.Id,
                        Price       = (decimal) 18.0, ProductName = "鍵盤"
                    }
                };
                dbContext.Employees.Add(employee1);

                var employee2 = new Employee
                {
                    Id       = Guid.NewGuid(),
                    Name     = "小英",
                    Age      = 23,
                    CreateAt = new DateTime(1909, 1, 2),
                    Identity = new Identity {Account = "James", Password = "123456"}
                };

                dbContext.Employees.Add(employee2);
                var employee3 = new Employee
                {
                    Id       = Guid.NewGuid(),
                    Name     = "小明",
                    Age      = 33,
                    CreateAt = new DateTime(2011, 2, 2),
                    Identity = new Identity {Account = "JOJO", Password = "123456"}
                };
                dbContext.Employees.Add(employee3);

                dbContext.SaveChanges();
            }
        }

        [TestMethod]
        public void 無法使用SQL分組()
        {
            ////無法使用SQL分組
            //using (var dbContext = new TestDbContext())
            //{
            //    dbContext.Database.Log = Console.WriteLine;
            //    var employeeGroups = dbContext.Employees
            //                                  .AsNoTracking()
            //                                  .GroupBy(p => p.Age)
            //                                  .ToList()
            //        ;
            //}

            var expected = new[]
            {
                new {Name = "小明", Age = 33},
                new {Name = "小英", Age = 23},
                new {Name = "小章", Age = 18}
            };

            using (var dbContext = new TestDbContext())
            {
                dbContext.Configuration.LazyLoadingEnabled   = false;
                dbContext.Configuration.ProxyCreationEnabled = false;
                var employeeGroups = dbContext.Employees
                                              .AsNoTracking()
                                              .ToList()
                                              .GroupBy(p => p.Age)
                    ;
                var employee = employeeGroups.First().First();

                employee.Should().BeEquivalentTo(expected[0]);
            }
        }

        [TestMethod]
        public void 無法使用SQL排序()
        {
            //////無法使用SQL排序
            ////using (var dbContext = new TestDbContext())
            ////{
            ////    var employees = dbContext.Employees.AsNoTracking().OrderBy(p => p.Name).ToList();
            //}

            var expected = new[]
            {
                new {Name = "小章", Age = 18},
                new {Name = "小英", Age = 23},
                new {Name = "小明", Age = 33}
            };
            using (var dbContext = new TestDbContext())
            {
                dbContext.Configuration.LazyLoadingEnabled   = false;
                dbContext.Configuration.ProxyCreationEnabled = false;

                var employees = dbContext.Employees
                                         .AsNoTracking()
                                         .ToList()
                                         .OrderBy(p => p.Age)
                    ;
                employees.Should().BeEquivalentTo(expected, option =>
                                                            {
                                                                option.WithStrictOrdering();
                                                                return option;
                                                            });
            }
        }

        [TestMethod]
        public void 無法直接投影集合_1()
        {
            ////無法直接投影集合
            //using (var dbContext = new TestDbContext())
            //{
            //    var orders = dbContext.Employees.Select(p => p.Orders).AsNoTracking().ToList();
            //}
            var expected = new[]
            {
                new
                {
                    Name = "小章", Age = 18, Orders = new[]
                    {
                        new {Price = (decimal) 20.00, ProductName = "滑鼠"},
                        new {Price = (decimal) 18.00, ProductName = "鍵盤"}
                    }
                }
            };

            using (var dbContext = new TestDbContext())
            {
                dbContext.Configuration.LazyLoadingEnabled   = false;
                dbContext.Configuration.ProxyCreationEnabled = false;
                var employees = dbContext.Employees
                                         .SelectMany(o => o.Orders, (employee, order) => new
                                         {
                                             employee.Id,
                                             employee.Age,
                                             employee.Name,
                                             Order = new {order.Id, order.Price, order.ProductName}
                                         })
                                         .AsNoTracking()
                                         .ToList()
                    ;

                var group = employees.GroupBy(e => new {e.Id, e.Name, e.Age},
                                              e => e.Order,
                                              (e, o) => new {e.Id, e.Name, e.Age, Orders = o})
                                     .ToList()
                    ;

                group.Should()
                     .BeEquivalentTo(expected, option =>
                                               {
                                                   option.WithStrictOrdering();
                                                   return option;
                                               });
            }
        }

        [TestMethod]
        public void 無法直接投影集合2()
        {
            var expected = new[]
            {
                new
                {
                    //Name = "小章", Age = 18,
                    Name = "小章", Age = 18, Orders = new[]
                    {
                        new {Price = (decimal) 20.00, ProductName = "滑鼠"},
                        new {Price = (decimal) 10.00, ProductName = "鍵盤"}
                    }
                }
            };

            using (var dbContext = new TestDbContext())
            {
                var employees = (from employee in dbContext.Employees
                                 join order in dbContext.Orders on employee.Id equals order.Employee_Id into orders
                                 from order in orders.DefaultIfEmpty()
                                 select new
                                 {
                                     employee.Id,
                                     employee.Name,
                                     employee.Age,
                                     Order = order == null
                                                 ? null
                                                 : new
                                                 {
                                                     order.Id,
                                                     order.Price,
                                                     order.ProductName
                                                 }
                                 }).ToList();

                var result = new Dictionary<Guid, EmployeeViewModel>();
                foreach (var element in employees)
                {
                    var employee = new EmployeeViewModel
                    {
                        Id   = element.Id,
                        Name = element.Name,
                        Age  = element.Age.Value
                    };
                    OrderViewModel order = null;
                    if (element.Order != null)
                    {
                        order = new OrderViewModel
                        {
                            Id          = element.Order.Id,
                            ProductName = element.Order.ProductName
                        };
                    }

                    if (!result.ContainsKey(element.Id))
                    {
                        result.Add(element.Id, employee);
                    }

                    if (order != null)
                    {
                        result[element.Id].Orders.Add(order);
                    }
                }
            }
        }

        [TestMethod]
        public void 過濾使用參數()
        {
            var name = "小章";
            using (var dbContext = new TestDbContext())
            {
                var employee = dbContext.Employees
                                        .Where(p => p.Name == name)
                                        .AsNoTracking()
                                        .FirstOrDefault();
                Assert.AreEqual(name, employee.Name);
            }
        }

        internal class OrderViewModel
        {
            public Guid Id { get; set; }

            public string ProductName { get; set; }
        }

        internal class EmployeeViewModel
        {
            public Guid Id { get; set; }

            public string Name { get; set; }

            public int Age { get; set; }

            public ICollection<OrderViewModel> Orders { get; internal set; }

            public EmployeeViewModel()
            {
                this.Orders = new HashSet<OrderViewModel>();
            }
        }
    }
}