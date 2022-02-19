using System.Threading.Tasks;
using BddPipe;
using BddPipe.Model;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static BddPipe.Runner;
using BddPipe.Recipe;

namespace Lab.BDD.Pipe.TestProject;

[TestClass]
public class UnitTest1
{
    [TestMethod]
    public void 相加兩個數字()
    {
        Scenario()
            .Given("有兩個數字", () => new { firstNumber = (decimal)5, secondNumber = (decimal)10 })
            .When("按下相加", setup =>
            {
                var calculation = new Calculation();
                return calculation.Add(setup.firstNumber, setup.secondNumber);
            })
            .Then("預期得到", actual =>
            {
                var expected = 15;
                Assert.AreEqual(expected, actual);
            })
            .Run();
    }
}

public class Calculation
{
    public decimal Add(decimal firstNumber, decimal secondNumber)
    {
        return firstNumber + secondNumber;
    }
}