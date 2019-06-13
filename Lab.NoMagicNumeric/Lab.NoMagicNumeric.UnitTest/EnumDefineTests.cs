using System;
using System.Linq;
using FluentAssertions;
using Lab.NoMagicNumeric.DAL;
using Lab.NoMagicNumeric.EntityModel.DAL;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lab.NoMagicNumeric.UnitTest
{
    [TestClass]
    public class EnumDefineTests
    {
        [TestMethod]
        public void EF_Select_Mapping()
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
                                              DefineManager.GetLookupByName<EnumTransfer>()[p.IsTransform]
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

        private DefineAttribute GetEnumApprove(string key)
        {
            var defineAttributes = DefineManager.GetLookupByName<EnumApprove>();
            Enum.TryParse(key, out EnumApprove value);
            return defineAttributes[value.ToString()];
        }

        [TestMethod]
        public void GetLookupByName_EnumApprove_Approve()
        {
            var description = DefineManager.GetLookupByName(typeof(EnumApprove), "Approve").Description;
            Assert.AreEqual("已核准", description);
        }

        [TestMethod]
        public void GetLookupByName_EnumApprove_Open()
        {
            var description = DefineManager.GetLookupByName<EnumApprove>()["Open"].Description;
            Assert.AreEqual("已開立", description);
        }

        [TestMethod]
        public void GetLookupByName_EnumTransfer_N()
        {
            var description = DefineManager.GetLookupByName(typeof(EnumTransfer), "N").Description;
            Assert.AreEqual("未轉換", description);
        }

        [TestMethod]
        public void GetLookupByName_EnumTransfer_Y()
        {
            var description = DefineManager.GetLookupByName<EnumTransfer>("Y").Description;
            Assert.AreEqual("已轉換", description);
        }

        [TestMethod]
        public void GetLookupByName_Extension_Test()
        {
            var description = EnumTransfer.N.GetDefineByName().Description;
            Assert.AreEqual("未轉換", description);
        }

        [TestMethod]
        public void GetLookupByValue_EnumApprove_Approve()
        {
            var description = DefineManager.GetLookupByValue(typeof(EnumApprove), "99").Description;
            Assert.AreEqual("已核准", description);
        }

        [TestMethod]
        public void GetLookupByValue_EnumApprove_Open()
        {
            var description = DefineManager.GetLookupByValue<EnumApprove>()["10"].Description;
            Assert.AreEqual("已開立", description);
        }

        [TestMethod]
        public void GetLookupByValue_EnumTransfer_N()
        {
            var description = DefineManager.GetLookupByValue(typeof(EnumTransfer), "0").Description;
            Assert.AreEqual("未轉換", description);
        }

        [TestMethod]
        public void GetLookupByValue_EnumTransfer_Y()
        {
            var description = DefineManager.GetLookupByValue<EnumTransfer>("1").Description;
            Assert.AreEqual("已轉換", description);
        }
    }
}