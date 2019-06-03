using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TechTalk.SpecFlow;

namespace Lab.SafeContext.UniTest
{
    [Binding]
    [Scope(Feature = "實作Steps")]
    public class 實作StepsSteps : Steps
    {
        private ScenarioStepContext _stepContext;

        public ScenarioStepContext StepContext
        {
            get => this._stepContext ?? (this._stepContext = this.ScenarioContext.StepContext);

            //get => this._stepContext ?? (this._stepContext =this.ScenarioContext.ScenarioContainer.Resolve<IContextManager>().StepContext);
            set => this._stepContext = value;
        }

        [Given(@"I also have entered (.*) into the calculator")]
        public void GivenIAlsoHaveEnteredIntoTheCalculator(int secondNumber)
        {
            var scenarioContext = this.ScenarioContext.Get<string>( "ScenarioContext");
            var featureContext = this.FeatureContext.Get<string>( "FeatureContext");
            var stepContext = this.StepContext.Get<string>( "StepContext");

            Console.WriteLine(scenarioContext);
            Console.WriteLine(featureContext);
            Console.WriteLine(stepContext);
            this.StepContext.Set(secondNumber, "secondNumber");
        }

        [Given(@"I have entered (.*) into the calculator")]
        public void GivenIHaveEnteredIntoTheCalculator(int firstNumber)
        {
            this.StepContext.Set(firstNumber, "firstNumber");
        }

        [Then(@"the result should be (.*) on the screen")]
        public void ThenTheResultShouldBeOnTheScreen(int expected)
        {
            var actual = this.StepContext.Get<int>("actual");
            Assert.AreEqual(expected, actual);
        }

        [When(@"I press add")]
        public void WhenIPressAdd()
        {
            var calculation  = new Calculation();
            var firstNumber  = this.StepContext.Get<int>("firstNumber");
            var secondNumber = this.StepContext.Get<int>("secondNumber");
            var actual       = calculation.Add(firstNumber, secondNumber);
            this.StepContext.Set(actual, "actual");
        }
    }
}