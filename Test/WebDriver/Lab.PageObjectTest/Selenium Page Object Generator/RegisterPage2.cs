using System;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace Selenium_Page_Object_Generator
{

    internal class RegisterPage2
    {
        private IWebDriver driver;

        public RegisterPage2(IWebDriver driver)
        {
            this.driver = driver;
        }

        /*
         * Email
         * ***************************************************************
         */

        public IWebElement GetEmailElement()
        {
            return driver.FindElement(By.Name("ctl00$GeneralContent$C067$ctl00$ctl00$tbEmail"));
        }

        public String GetEmail()
        {
            return GetEmailElement().GetAttribute("value");
        }

        public void SetEmail(String value)
        {
            GetEmailElement().SendKeys(value);
        }

        /*
         * Firstname
         * ***************************************************************
         */

        public IWebElement GetFirstnameElement()
        {
            return driver.FindElement(By.Id("GeneralContent_C067_ctl00_ctl00_tbFirstName"));
        }

        public String GetFirstname()
        {
            return GetFirstnameElement().GetAttribute("value");
        }

        public void SetFirstname(String value)
        {
            GetFirstnameElement().SendKeys(value);
        }

        /*
         * Lastname
         * ***************************************************************
         */

        public IWebElement GetLastnameElement()
        {
            return driver.FindElement(By.Id("GeneralContent_C067_ctl00_ctl00_tbLastName"));
        }

        public String GetLastname()
        {
            return GetLastnameElement().GetAttribute("value");
        }

        public void SetLastname(String value)
        {
            GetLastnameElement().SendKeys(value);
        }

        /*
         * Company
         * ***************************************************************
         */

        public IWebElement GetCompanyElement()
        {
            return driver.FindElement(By.Id("GeneralContent_C067_ctl00_ctl00_tbCompanyName"));
        }

        public String GetCompany()
        {
            return GetCompanyElement().GetAttribute("value");
        }

        public void SetCompany(String value)
        {
            GetCompanyElement().SendKeys(value);
        }

        /*
         * Country
         * ***************************************************************
         */

        public IWebElement GetCountryElement()
        {
            return driver.FindElement(By.Id("GeneralContent_C067_ctl00_ctl00_ddlCountry"));
        }

        public SelectElement GetCountrySelect()
        {
            return new SelectElement(GetCountryElement());
        }

        public String GetCountryText()
        {
            return GetCountrySelect().SelectedOption.Text;
        }

        public String GetCountryValue()
        {
            return GetCountrySelect().SelectedOption.GetAttribute("value");
        }

        public void SetCountryByValue(String value)
        {
            GetCountrySelect().SelectByValue(value);
        }

        public void SetCountryByText(String text)
        {
            GetCountrySelect().SelectByText(text);
        }

        /*
         * Phone
         * ***************************************************************
         */

        public IWebElement GetPhoneElement()
        {
            return driver.FindElement(By.Id("GeneralContent_C067_ctl00_ctl00_tbPhone"));
        }

        public String GetPhone()
        {
            return GetPhoneElement().GetAttribute("value");
        }

        public void SetPhone(String value)
        {
            GetPhoneElement().SendKeys(value);
        }

        /*
         * Createaccount
         * ***************************************************************
         */

        public IWebElement GetCreateaccountElement()
        {
            return driver.FindElement(By.Id("GeneralContent_C067_ctl00_ctl00_btnSubmit"));
        }

        public void ClickCreateaccount()
        {
            GetCreateaccountElement().Click();
        }

    }
}