using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NetFx48
{
    [TestClass]
    public class SurveyKeyPerFileConfigurationTests
    {
        [TestMethod]
        public void 手動實例化ConfigurationBuilder()
        {
            var expected     = "我是檔案內容";
            var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "keys/aws/web");
            var configBuilder = new ConfigurationBuilder()
                                .SetBasePath(Directory.GetCurrentDirectory())
                                .AddKeyPerFile(folderPath,false);
            var configRoot = configBuilder.Build();

            //讀取組態
            var actual = configRoot["NewFile1.txt"];
            Console.WriteLine($"NewFile1.txt = {actual}");
            Assert.AreEqual(expected,actual);
        }
    }
}