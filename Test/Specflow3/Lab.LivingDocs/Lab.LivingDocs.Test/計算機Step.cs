using TechTalk.SpecFlow;

namespace Lab.LivingDocs.Test;

[Binding]
public class 計算機Step : Steps
{
    [Given(@"第一個數字為 (.*)")]
    public void Given第一個數字為(double firstNumber)
    {
        this.ScenarioContext.Set(firstNumber, "firstNumber");
    }

    [Given(@"第二個數字為 (.*)")]
    public void Given第二個數字為(double secondNumber)
    {
        this.ScenarioContext.Set(secondNumber, "secondNumber");
    }

    [Then(@"結果應該為 (.*)")]
    public void Then結果應該為(double expected)
    {
        var actual = this.ScenarioContext.Get<double>("actual");
        Assert.AreEqual(expected, actual);
    }

    [When(@"兩個數字相加")]
    public void When兩個數字相加()
    {
        var firstNumber = this.ScenarioContext.Get<double>("firstNumber");
        var secondNumber = this.ScenarioContext.Get<double>("secondNumber");
        var calculation = new Calculation();
        var actual = calculation.Add(firstNumber, secondNumber);
        this.ScenarioContext.Set(actual, "actual");
    }
}