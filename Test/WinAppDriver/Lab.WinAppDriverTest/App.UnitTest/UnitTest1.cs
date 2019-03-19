using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium.Appium.Windows;
using OpenQA.Selenium.Remote;

namespace App.UnitTest
{
    [TestClass]
    public class UnitTest1
    {
        protected internal static WindowsDriver<WindowsElement> WindowsDriver;

        [ClassInitialize]
        public static void Setup(TestContext context)
        {
            var projectName = "App";
            string solutionPath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..\\..\\..\\"));
            var targetAppPath = Path.Combine(solutionPath, projectName, "bin", "debug", "app.exe");

            DesiredCapabilities appCapabilities = new DesiredCapabilities();
            appCapabilities.SetCapability("app", targetAppPath);
            //appCapabilities.SetCapability("app", @"C:\Program Files (x86)\Progress\Telerik UI for WinForms R1 2019\Examples\QuickStart\bin\TelerikExamples.exe");

            WindowsDriver = new WindowsDriver<WindowsElement>(new Uri("http://127.0.0.1:4723"), appCapabilities);
        }

        [ClassCleanup]
        public static void Cleanup()
        {
            WindowsDriver.CloseApp();
            WindowsDriver.Dispose();
        }

        [TestMethod] public void 輸入帳號密碼_按下登入_預期得到一個彈跳視窗並呈現Hiyao()
        {
            WindowsDriver.FindElementByAccessibilityId("Id_TextBox").SendKeys("yao");
            WindowsDriver.FindElementByAccessibilityId("Password_TextBox").SendKeys("123456");
            WindowsDriver.FindElementByAccessibilityId("Login_Button").Click();

            var messageBox = WindowsDriver.FindElementByClassName("#32770");
            var title = messageBox.Text;
            var messageText = messageBox.FindElementByXPath("//Text[@Name='Hi~yao']").Text;

            Assert.AreEqual("Title", title);
            Assert.AreEqual("Hi~yao", messageText);

            WindowsDriver.FindElementByXPath("//Button[@Name='OK']").Click();
        }
    }
}