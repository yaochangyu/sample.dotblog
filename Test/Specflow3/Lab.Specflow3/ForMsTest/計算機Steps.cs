using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TechTalk.SpecFlow;

namespace ForMsTest
{
    [Binding]
    public class 計算機Steps
    {
        [Given(@"I have entered (.*) into the calculator")]
        public void GivenIHaveEnteredIntoTheCalculator(int firstNumber)
        {
            ScenarioContext.Current.Set(firstNumber,"firstNumber");
        }
        
        [Given(@"I have also entered (.*) into the calculator")]
        public void GivenIHaveAlsoEnteredIntoTheCalculator(int secondNumber)
        {
            ScenarioContext.Current.Set(secondNumber,"secondNumber");
        }
        
        [When(@"I press add")]
        public void WhenIPressAdd()
        {
            var firstNumber = ScenarioContext.Current.Get<int>("firstNumber");
            var secondNumber = ScenarioContext.Current.Get<int>("secondNumber");
            var calculation = new Calculation();
            var actual = calculation.Add(firstNumber, secondNumber);
            ScenarioContext.Current.Set(actual,"actual");
        }
        
        [Then(@"the result should be (.*) on the screen")]
        public void ThenTheResultShouldBeOnTheScreen(int expected)
        {
            var actual = ScenarioContext.Current.Get<int>("actual");
            Assert.AreEqual(expected,actual);
        }
    }
    public class Calculation
    {
        public int Add(int firstNumber, int secondNumber)
        {
            return firstNumber + secondNumber;
        }
    }
}
