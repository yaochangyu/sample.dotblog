using System.Linq;
using FluentAssertions;
using Lab.NoMagicNumeric.DAL;
using Lab.NoMagicNumeric.EntityModel.DAL;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lab.NoMagicNumeric.UnitTest
{
    [TestClass]
    public class ClassDefineTests
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
                                              DefineManager.GetLookup<TransferStatus>()[p.IsTransform].Description,
                                          p.Status,
                                          StatusDescription =
                                              DefineManager.GetLookup<ApproveStatus>()[p.Status].Description
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

        [TestMethod]
        public void GetLookup_By_ApproveStatus_Approve()
        {
            var description = DefineManager.GetLookup<ApproveStatus>()["99"].Description;
            Assert.AreEqual("已核准", description);
        }

        [TestMethod]
        public void GetLookup_By_ApproveStatus_Open()
        {
            var description = DefineManager.GetLookup<ApproveStatus>()["10"].Description;
            Assert.AreEqual("已開立", description);
        }

        [TestMethod]
        public void GetLookup_By_TransferStatus_N()
        {
            var description = DefineManager.GetLookup<TransferStatus>()["N"].Description;
            Assert.AreEqual("未轉換", description);
        }

        [TestMethod]
        public void GetLookup_By_TransferStatus_Y()
        {
            var description = DefineManager.GetLookup<TransferStatus>()["Y"].Description;
            Assert.AreEqual("已轉換", description);
        }
    }
}