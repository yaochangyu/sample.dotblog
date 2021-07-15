using System.IO;
using System.Threading;
using Lab.LineBot.SDK;
using Lab.LineBot.SDK.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lab.LineNotify.Service.TestProject
{
    [TestClass]
    public class LineNotifyProviderTests
    {
        [TestMethod]
        public void 發送訊息和表情()
        {
            var provider = new LineNotifyProvider();
            var response = provider.NotifyAsync(new NotifyWithStickerRequest
                                   {
                                       AccessToken      = "3lZwryen62tiQ4BKfh3uH3NFoFtALF4SrfgLWMIKrXh",
                                       Message          = "HI~請給我黃金",
                                       StickerPackageId = 1.ToString(),
                                       StickerId        = 113.ToString()
                                   }, CancellationToken.None)
                                   .Result;
            Assert.AreEqual(200, response.Status);
        }

        [TestMethod]
        public void 發送訊息和圖片()
        {
            var provider = new LineNotifyProvider();
            var response = provider.NotifyAsync(new NotifyWithImageRequest
                                   {
                                       AccessToken = "3lZwryen62tiQ4BKfh3uH3NFoFtALF4SrfgLWMIKrXh",
                                       Message = "HI~請給我黃金",
                                       FilePath = "1.jpg",
                                       FileBytes = File.ReadAllBytes("1.jpg")
                                   }, CancellationToken.None)
                                   .Result;
            Assert.AreEqual(200, response.Status);
        }
    }
}