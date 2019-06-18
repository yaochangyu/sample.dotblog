using Microsoft.VisualStudio.TestTools.UnitTesting;
using TechTalk.SpecFlow;

namespace Lab.CallOtherStep.UnitTest
{
    [Binding]
    [Scope(Feature = "計算機V1")]
    public class 計算機V1Steps : Steps
    {
        [Given(@"I also have entered (.*) into the calculator")]
        public void GivenIAlsoHaveEnteredIntoTheCalculator(decimal secondNumber)
        {
            this.ScenarioContext.Set(secondNumber, "secondNumber");
        }

        [Given(@"I have entered (.*) into the calculator")]
        public void GivenIHaveEnteredIntoTheCalculator(decimal firstNumber)
        {
            this.ScenarioContext.Set(firstNumber, "firstNumber");
        }

        [Given(@"I press add and the result should be success")]
        public void GivenIPressAddAndTheResultShouldBeSuccess()
        {
            decimal firstNumber  = 70;
            decimal secondNumber = 50;
            decimal expected     = 120;
            this.Given($@"I have entered {firstNumber} into the calculator");
            this.Given($@"I also have entered {secondNumber} into the calculator");
            this.When(@"I press add");
            this.Then($@"the result should be {expected} on the screen");
        }

        [Then(@"the result should be (.*) on the screen")]
        public void ThenTheResultShouldBeOnTheScreen(decimal expected)
        {
            var actual = this.ScenarioContext.Get<decimal>("actual");
            Assert.AreEqual(expected, actual);
        }

        [When(@"I press add")]
        public void WhenIPressAdd()
        {
            var firstNumber  = this.ScenarioContext.Get<decimal>("firstNumber");
            var secondNumber = this.ScenarioContext.Get<decimal>("secondNumber");
            var calculation  = new Calculation();
            var actual       = calculation.Add(firstNumber, secondNumber);
            this.ScenarioContext.Set(actual, "actual");
        }
    }
}