using Microsoft.VisualStudio.TestTools.UnitTesting;
using TechTalk.SpecFlow;

namespace Lab.AllureReport4Specflow;

[Binding]
public class 計算機Step : Steps
{
    [Given(@"the first number is (.*)")]
    public void GivenTheFirstNumberIs(double firstNumber)
    {
        this.ScenarioContext.Set(firstNumber, "firstNumber");
    }

    [Given(@"the second number is (.*)")]
    public void GivenTheSecondNumberIs(double secondNumber)
    {
        this.ScenarioContext.Set(secondNumber, "secondNumber");
    }

    [Then(@"the result should be (.*)")]
    public void ThenTheResultShouldBe(int expected)
    {
        var actual = this.ScenarioContext.Get<double>("actual");
        Assert.AreEqual(expected, actual);
    }

    [When(@"the two numbers are added")]
    public void WhenTheTwoNumbersAreAdded()
    {
        var firstNumber = this.ScenarioContext.Get<double>("firstNumber");
        var secondNumber = this.ScenarioContext.Get<double>("secondNumber");
        var calculation = new Calculation();
        var actual = calculation.Add(firstNumber, secondNumber);
        this.ScenarioContext.Set(actual, "actual");
    }
}