using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Entity;
using System.Data.Entity.Core;
using System.Linq;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Lab.EF6.AlwaysEncrypt.UnitTest.EntityModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lab.EF6.AlwaysEncrypt.UnitTest
{
    [TestClass]
    public class EF6_UnitTest
    {
        [TestMethod]
        public void AutoMapperDynamic()
        {
            dynamic source  = new {Id = 1, Name = "yao", CreteAt = DateTime.Now, ModifyAt = DateTime.Now};
            var     config  = new MapperConfiguration(cfg => { });
            var     mapper  = config.CreateMapper();
            var     retUser = DynamicToStatic.ToStatic<Employee1>(source);
        }

        [TestMethod]
        public void AutoMapperEF6()
        {
            //Mapper.CreateMap<Employee, EmployeeDto>()
            //      .ForMember(d => d.FullName, opt => opt.MapFrom(src => src.FirstName + " " + src.LastName));

            var name = "yao";
            using (var dbContext = new TestDbContext())
            {
                var config = new MapperConfiguration(cfg =>
                                                     {
                                                         cfg.CreateMissingTypeMaps = false;
                                                         cfg.CreateMap<Employee, ViewModel.Employee>();
                                                     });
                var mapper = config.CreateMapper();
                var employees = dbContext.Employees
                                         .ProjectTo<ViewModel.Employee>(config)
                                         .AsNoTracking()
                                         .ToList()
                    ;
            }
        }

        [TestMethod]
        public void 原始()
        {
            var name = "yao";
            using (var dbContext = new TestDbContext())
            {
                var original = dbContext.Employees
                                        .AsNoTracking()
                                        .ToList();
            }
        }

        [TestMethod]
        public void 搜尋()
        {
            var name = "yao";
            using (var dbContext = new TestDbContext())
            {
                var employees = dbContext.Employees
                                         .Select(p => new ViewModel.Employee
                                         {
                                             Id       = p.Id,
                                             Name     = p.Name,
                                             CreateAt = p.CreateAt
                                         })
                                         .Where(p => p.Name == name)
                                         .AsNoTracking()
                                         .ToList();
            }
        }

        [TestMethod]
        [ExpectedException(typeof(EntityCommandExecutionException))]
        public void 資料庫欄位NULL_轉型Bonus失敗()
        {
            var name = "demo";
            using (var dbContext = new TestDbContext())
            {
                var employees = dbContext.Employees
                                         .Select(p => new ViewModel.Employee
                                         {
                                             Id       = p.Id,
                                             Name     = p.Name,
                                             CreateAt = p.CreateAt,
                                             Bonus    = p.Bonus ?? 0

                                             //Bonus = p.Bonus == null ? 0 : p.Bonus.Value,
                                             //Bonus    = p.Bonus.HasValue == false ? 0 : p.Bonus.Value,
                                         })
                                         .Where(p => p.Name == name)
                                         .AsNoTracking()
                                         .ToList();
            }
        }

        [TestMethod]
        [ExpectedException(typeof(EntityCommandExecutionException))]
        public void 資料庫欄位NULL_轉型ModifyAt失敗()
        {
            var name = "demo";
            using (var dbContext = new TestDbContext())
            {
                var employees = dbContext.Employees
                                         .Select(p => new ViewModel.Employee
                                         {
                                             Id       = p.Id,
                                             Name     = p.Name,
                                             CreateAt = p.CreateAt,
                                             ModifyAt = p.ModifyAt ?? DateTime.Now

                                             //Bonus = p.Bonus == null ? 0 : p.Bonus.Value,
                                             //Bonus    = p.Bonus.HasValue == false ? 0 : p.Bonus.Value,
                                         })
                                         .Where(p => p.Name == name)
                                         .AsNoTracking()
                                         .ToList();
            }
        }

        [TestMethod]
        public void 資料庫欄位NULL轉型_匿名型別轉型()
        {
            var name = "yao";
            using (var dbContext = new TestDbContext())
            {
                var employees = dbContext.Employees
                                         .Select(p => new
                                         {
                                             p.Id,
                                             p.Name,
                                             p.CreateAt,
                                             p.Bonus
                                         })
                                         .Where(p => p.Name == name)
                                         .AsNoTracking()
                                         .ToList()
                                         .Select(p => new ViewModel.Employee
                                         {
                                             Id       = p.Id,
                                             Name     = p.Name,
                                             CreateAt = p.CreateAt,
                                             Bonus    = p.Bonus ?? 0

                                             //Bonus = p.Bonus == null ? 0 : p.Bonus.Value,
                                             //Bonus    = p.Bonus.HasValue == false ? 0 : p.Bonus.Value,
                                         });
            }
        }

        [TestMethod]
        public void 資料庫欄位NULL轉型_匿名型別轉型1()
        {
            var name = "yao";
            using (var dbContext = new TestDbContext())
            {
                var employees = dbContext.Employees
                                         .Select(p => new
                                         {
                                             p.Id,
                                             p.Name,
                                             p.CreateAt,
                                             p.Bonus
                                         })
                                         .Where(p => p.Name == name)
                                         .AsNoTracking()
                                         .ToList()
                    ;
                dynamic employee = dbContext.Employees
                                            .Select(p => new
                                            {
                                                p.Id,
                                                p.Name,
                                                p.CreateAt,
                                                p.Bonus
                                            })
                                            .Where(p => p.Name == name)
                                            .AsNoTracking()
                                            .FirstOrDefault();

                //var config = new MapperConfiguration(cfg =>
                //                                     {
                //                                         cfg.CreateMissingTypeMaps = true;
                //                                     });
                //var mapper = config.CreateMapper();

                //var target = employees.Select(mapper.Map<Employee>)
                //                      .ToList();
                var config  = new MapperConfiguration(cfg => { });
                var mapper  = config.CreateMapper();
                var target  = mapper.Map<ViewModel.Employee>(employee);
                var targets = mapper.Map<IEnumerable<ViewModel.Employee>>(employees);

                //var retUser = Mapper.DynamicMap<Employee>(employees);
            }
        }

        [TestMethod]
        public void 對應_DateType()
        {
            var name = "yao";
            using (var dbContext = new TestDbContext())
            {
                var original = dbContext.Employees
                                        .Select(p => new ViewModel.Employee
                                        {
                                            Id       = p.Id,
                                            BirthDay = p.Birthday
                                        })
                                        .AsNoTracking()
                                        .ToList()
                    ;
            }
        }
    }

    public class Employee1
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public DateTime CreteAt { get; set; }

        public DateTime ModifyAt { get; set; }

        public int? Age { get; set; }
    }

    public static class DynamicToStatic
    {
        public static T ToStatic<T>(object expando)
        {
            var dictionary = new Dictionary<string, object>();
            foreach (PropertyDescriptor propertyDescriptor in TypeDescriptor.GetProperties(expando))
            {
                var obj = propertyDescriptor.GetValue(expando);
                dictionary.Add(propertyDescriptor.Name, obj);
            }

            var entity = Activator.CreateInstance<T>();

            //ExpandoObject implements dictionary
            foreach (var entry in dictionary)
            {
                var propertyInfo = entity.GetType().GetProperty(entry.Key);
                if (propertyInfo != null)
                {
                    propertyInfo.SetValue(entity, entry.Value, null);
                }
            }

            return entity;
        }
    }
}