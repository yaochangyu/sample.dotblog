using System;
using System.Diagnostics;
using System.Windows.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lab.ExceptionStack.BLL.UnitTest
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            try
            {
                var calculation = new Calculation();
                calculation.Add(1, 1);
                calculation.Sub(1, 1);
            }
            catch (Exception e)
            {
                var description = e.GetCurrentErrorDescription();
                MessageBox.Show(description.Description);
            }
        }
    }
}
