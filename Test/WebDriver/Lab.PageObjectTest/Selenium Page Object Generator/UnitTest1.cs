using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.PageObjects;

namespace Selenium_Page_Object_Generator
{
    [TestClass]
    public class UnitTest1
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

        [TestMethod]
        public void TestMethod1()
        {
            driver.Navigate().GoToUrl("https://www.google.com");
            Thread.Sleep(100);
            driver.FindElement(By.Name("q")).Clear();
            driver.FindElement(By.Name("q")).SendKeys("selenium webdriver C#");
            driver.FindElement(By.Name("q")).SendKeys(Keys.Enter);
            var element = driver.FindElement(By.XPath("//div[@id=\'rso\']/div/div/div/div/div/div/a/h3"));
            Assert.AreEqual("Selenium C# Webdriver Tutorial: NUnit Example - Guru99", element.Text);
        }

        [TestMethod]
        public void TestMethod2()
        {
            driver.Navigate().GoToUrl("https://www.telerik.com/login/v2/telerik#register");
            var register = PageFactory.InitElements<RegisterPage>(driver);

            register.SetEmail2EmailField("aaa@gmail.com")
                    .SetFirstNameTextField("first name")
                    .SetLastNameTextField("last name")
                    .SetCompany3TextField("company")
                    .SetCountryDropDownListField("Taiwan")
                    .SetPhoneTextField("1234567890");
        }

        [TestMethod]
        public void TestMethod3()
        {
            driver.Navigate().GoToUrl("https://www.telerik.com/login/v2/telerik#register");
            var data = new Dictionary<string, string>
            {
                {"EMAIL_2", "aa@bb.cc"},
                {"FIRST_NAME", "FirstName"},
                {"LAST_NAME", "LastName"},
                {"PHONE", "Phone"},
                {"COMPANY_3", "Company"},
                {"COUNTRY", "Taiwan"}
            };
            var register = new RegisterPage(driver, data);
            PageFactory.InitElements(driver, register);
            register.Fill();
        }

        [TestMethod]
        public void TestMethod4()
        {
            driver.Navigate().GoToUrl("https://www.telerik.com/login/v2/telerik#register");
            var register = new RegisterPage2(driver);

            register.SetEmail("aaa@gmail.com");
            register.SetFirstname("first name");
            register.SetLastname("last name");
            register.SetCompany("company");
            register.SetCountryByText("Taiwan");
            register.SetPhone("1234567890");
        }
    }
}