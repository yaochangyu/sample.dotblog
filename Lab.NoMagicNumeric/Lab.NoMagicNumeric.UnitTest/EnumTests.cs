using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Lab.NoMagicNumeric.DAL;
using Lab.NoMagicNumeric.EntityModel.DAL;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lab.NoMagicNumeric.UnitTest
{
    [TestClass]
    public partial class EnumTests
    {
        [AssemblyCleanup]
        public static void AssemblyCleanup()
        {
            using (var dbContext = new LabDbContext())
            {
                if (dbContext.Database.Exists())
                {
                    dbContext.Database.Delete();
                }
            }
        }

        [AssemblyInitialize]
        public static void AssemblyInitialize(TestContext context)
        {
            using (var dbContext = new LabDbContext())
            {
                var toDb = new List<Order>
                {
                    new Order {Id = Guid.NewGuid(), IsTransform = "Y", Status = "99"},
                    new Order {Id = Guid.NewGuid(), IsTransform = "N", Status = "10"}
                };
                dbContext.Orders.AddRange(toDb);
                dbContext.SaveChanges();
            }
        }

        private DefineAttribute GetEnumApprove(string key)
        {
            var defineAttributes = DefineManager.GetEnumLookup<EnumApprove>();
            System.Enum.TryParse(key, out EnumApprove value);
            return defineAttributes[value.ToString()];
        }

        [TestMethod]
        public void GetEnumLookup_By_EnumApprove_Approve()
        {
            var description = DefineManager.GetEnumLookup(typeof(EnumApprove))["Approve"].Description;
            Assert.AreEqual("已核准", description);
        }

        [TestMethod]
        public void GetEnumLookup_By_EnumApprove_Open()
        {
            var description = DefineManager.GetEnumLookup<EnumApprove>()["Open"].Description;
            Assert.AreEqual("已開立", description);
        }

        [TestMethod]
        public void GetEnumLookup_By_EnumTransfer_N()
        {
            var description = DefineManager.GetEnumLookup<EnumTransfer>("N").Description;
            Assert.AreEqual("未轉換", description);
        }

        [TestMethod]
        public void GetEnumLookup_By_EnumTransfer_Y()
        {
            var description = DefineManager.GetEnumLookup<EnumTransfer>("Y").Description;
            Assert.AreEqual("已轉換", description);
        }

        [TestMethod]
        public void GetEnumLookup_Extension_Test()
        {
            var description = EnumTransfer.N.GetDefine().Description;
            Assert.AreEqual("未轉換", description);
        }

        [TestMethod]
        public void GetEnumLookup_Test()
        {
            var transferLookup = DefineManager.GetEnumLookup<EnumTransfer>();
            var description    = transferLookup[EnumTransfer.Y.ToString()].Description;
        }

        [TestMethod]
        public void Mapping()
        {
            var expected = new[]
            {
                new
                {
                    IsTransform          = "N",
                    TransformDescription = "未轉換",
                    Status               = "10",
                    StatusDescription    = "已開立"
                },
                new
                {
                    IsTransform          = "Y",
                    TransformDescription = "已轉換",
                    Status               = "99",
                    StatusDescription    = "已核准"
                }
            };
            using (var dbContext = new LabDbContext())
            {
                var orders = dbContext.Orders
                                      .AsNoTracking()
                                      .ToList()
                                      .Select(p => new
                                      {
                                          p.Id,
                                          p.IsTransform,
                                          TransformDescription =
                                              DefineManager.GetEnumLookup<EnumTransfer>()[p.IsTransform]
                                                           .Description,
                                          p.Status,
                                          StatusDescription = this.GetEnumApprove(p.Status).Description
                                      })
                                      .ToList()
                    ;
                orders.Should()
                      .BeEquivalentTo(expected, option =>
                                                {
                                                    option.WithoutStrictOrdering();
                                                    return option;
                                                });
            
            }
        }
    }
}