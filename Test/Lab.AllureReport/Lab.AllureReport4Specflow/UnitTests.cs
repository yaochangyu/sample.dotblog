using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lab.AllureReport4Specflow;

[TestClass]
public class UnitTests
{
    [TestMethod]
    [DataRow(50,70,120)]
    public void 相加兩個數字(double firstNumber, double secondNumber, double expected)
    {
        var calculation = new Calculation();
        var actual = calculation.Add(firstNumber, secondNumber);
        Assert.AreEqual(expected, actual);
    }
}