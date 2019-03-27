using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium.Appium.Windows;

namespace App.UnitTest
{
    internal class LoginPage
    {
        protected internal WindowsDriver<WindowsElement> _driver;

        public LoginPage(WindowsDriver<WindowsElement> driver)
        {
            this._driver = driver;
        }

        // id

        public WindowsElement IdElement => this._driver.FindElementByAccessibilityId("Id_TextBox");
        
        public LoginPage SetId(string id)
        {
            this.IdElement.SendKeys(id);
            return this;
        }

        // password

        public WindowsElement PasswordElement => this._driver.FindElementByAccessibilityId("Password_TextBox");
        
        public LoginPage SetPassword(string password)
        {
            this.PasswordElement.SendKeys(password);
            return this;
        }

        // login

        public WindowsElement LoginElement => this._driver.FindElementByAccessibilityId("Login_Button");

        public LoginPage ClickLogin()
        {
            this.LoginElement.Click();
            return this;
        }

        // ok button
        public WindowsElement OkElement => this._driver.FindElementByXPath("//Button[@Name='OK']");

        public LoginPage ClickOK()
        {
            this.OkElement.Click();
            return this;
        }

        // messagebox
        public WindowsElement MessageBoxElemnt => this._driver.FindElementByClassName("#32770");

        public LoginPage VerifyMessageBoxByName(string expected)
        {
            var messageText = this.MessageBoxElemnt.FindElementByXPath($"//Text[@Name='{expected}']").Text;
            Assert.AreEqual(expected, messageText);
            return this;
        }

        public LoginPage VerifyMessageBoxByTitle(string expected)
        {
            var messageText = this.MessageBoxElemnt.Text;
            Assert.AreEqual(expected, messageText);
            return this;
        }

    }
}