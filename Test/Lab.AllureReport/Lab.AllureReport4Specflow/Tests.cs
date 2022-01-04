using Allure.Commons;
using NUnit.Allure.Attributes;
using NUnit.Allure.Core;
using NUnit.Framework;
using Assert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;

namespace Lab.AllureReport4Specflow;

[TestFixture]
[AllureNUnit]
[AllureSubSuite("Example")]
[AllureSeverity(SeverityLevel.critical)]      
public class Tests
{
    [Test]
    [AllureTag("NUnit","Debug")]
    [AllureIssue("GitHub#1", "https://github.com/unickq/allure-nunit")]
    [AllureFeature("Core")]
    [TestCase(20, 50, 70)]
    public void 相加兩個數字(double firstNumber, double secondNumber, double expected)
    {
        var calculation = new Calculation();
        var actual = calculation.Add(firstNumber, secondNumber);
        Assert.AreEqual(expected, actual);
    }
}