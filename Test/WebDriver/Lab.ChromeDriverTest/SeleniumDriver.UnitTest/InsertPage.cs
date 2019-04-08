using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenQA.Selenium;

namespace SeleniumDriver.UnitTest
{
    class InsertPage
    {
        private IWebDriver driver;

        public InsertPage(IWebDriver driver)
        {
            this.driver = driver;
        }
        /*
        * Addnewrecord
        * ***************************************************************
        */

        public IWebElement GetAddnewrecordElement()
        {
            return driver.FindElement(By.XPath("//a[contains(text(),'Add new record')]"));
        }

        public InsertPage ClickAddnewrecord()
        {
            GetAddnewrecordElement().Click();
            return this;
        }



/*
 * ProductName
 * ***************************************************************
 */

        public IWebElement GetProductNameElement()
        {
            return driver.FindElement(By.Name("ProductName"));
        }

        public String GetProductName()
        {
            return GetProductNameElement().GetAttribute("value");
        }

        public void SetProductName(String value)
        {
            GetProductNameElement().SendKeys(value);
        }

/*
 * UnitPrice
 * ***************************************************************
 */

        public IWebElement GetUnitPriceElement()
        {
            return
                driver.FindElement(By.XPath("//div[contains(@class,'k-edit-form-container')]/div[4]/span[1]/span/input[1]"));
        }

        public String GetUnitPrice()
        {
            return GetUnitPriceElement().GetAttribute("value");
        }

        public void SetUnitPrice(String value)
        {
            GetUnitPriceElement().SendKeys(value);
        }

/*
 * UnitsInStock
 * ***************************************************************
 */

        public IWebElement GetUnitsInStockElement()
        {
            return
                driver.FindElement(By.XPath("//div[contains(@class,'k-edit-form-container')]/div[6]/span[1]/span/input[1]"));
        }

        public String GetUnitsInStock()
        {
            return GetUnitsInStockElement().GetAttribute("value");
        }

        public void SetUnitsInStock(String value)
        {
            GetUnitsInStockElement().SendKeys(value);
        }

/*
 * Update
 * ***************************************************************
 */

        public IWebElement GetUpdateElement()
        {
            return driver.FindElement(By.LinkText("Update"));
        }

        public void ClickUpdate()
        {
            GetUpdateElement().Click();
        }

/*
 * Cancel
 * ***************************************************************
 */

        public IWebElement GetCancelElement()
        {
            return driver.FindElement(By.LinkText("Cancel"));
        }

        public void ClickCancel()
        {
            GetCancelElement().Click();
        }
    }
}
