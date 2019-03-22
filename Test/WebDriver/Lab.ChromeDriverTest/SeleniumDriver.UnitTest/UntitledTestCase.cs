using System;
using System.Text;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.IE;
using OpenQA.Selenium.Support.UI;

namespace SeleniumDriver.UnitTest
{
    [TestClass]
    public class UntitledTestCase
    {
        private static IWebDriver driver;
        private static string baseURL;
        private bool acceptNextAlert = true;
        private StringBuilder verificationErrors;

        [ClassInitialize]
        public static void InitializeClass(TestContext testContext)
        {
            driver = new ChromeDriver();
            //driver = new FirefoxDriver();
            //driver = new EdgeDriver();
            baseURL = "https://www.katalon.com/";
        }

        [ClassCleanup]
        public static void CleanupClass()
        {
            try
            {
                //driver.Quit();// quit does not close the window
                //driver.Close();
                driver.Dispose();
            }
            catch (Exception)
            {
                // Ignore errors if unable to close the browser
            }
        }

        [TestInitialize]
        public void InitializeTest()
        {
            this.verificationErrors = new StringBuilder();
        }

        [TestCleanup]
        public void CleanupTest()
        {
            Assert.AreEqual("", this.verificationErrors.ToString());
        }

        [TestMethod]
        public void TheUntitledTestCaseTest()
        {
            driver.Navigate().GoToUrl("https://www.google.com");
            Thread.Sleep(100);
            driver.FindElement(By.Name("q")).Clear();
            driver.FindElement(By.Name("q")).SendKeys("selenium webdriver C#");
            driver.FindElement(By.Name("q")).SendKeys(Keys.Enter);
            var element = driver.FindElement(By.XPath("//div[@id=\'rso\']/div/div/div/div/div/div/a/h3"));
            Assert.AreEqual("Selenium C# Webdriver Tutorial: NUnit Example - Guru99", element.Text);
        }

        private bool IsElementPresent(By by)
        {
            try
            {
                driver.FindElement(by);
                return true;
            }
            catch (NoSuchElementException)
            {
                return false;
            }
        }

        private bool IsAlertPresent()
        {
            try
            {
                driver.SwitchTo().Alert();
                return true;
            }
            catch (NoAlertPresentException)
            {
                return false;
            }
        }

        private string CloseAlertAndGetItsText()
        {
            try
            {
                IAlert alert = driver.SwitchTo().Alert();
                string alertText = alert.Text;
                if (this.acceptNextAlert)
                {
                    alert.Accept();
                }
                else
                {
                    alert.Dismiss();
                }

                return alertText;
            }
            finally
            {
                this.acceptNextAlert = true;
            }
        }
    }
}
