using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TechTalk.SpecFlow;
using TechTalk.SpecFlow.Infrastructure;

namespace Lab.SafeContext
{
    [Binding]
    [Scope(Feature = "注入Context")]
    public class 注入ContextSteps
    {
        private ScenarioStepContext _stepContext;

        private FeatureContext FeatureContext { get; }

        private ScenarioContext ScenarioContext { get; }

        public ScenarioStepContext StepContext
        {
            //get => this._stepContext ?? (this._stepContext = this.ScenarioContext.StepContext);
            get => this._stepContext ?? (this._stepContext = this.ScenarioContext
                                                                 .ScenarioContainer
                                                                 .Resolve<IContextManager>()
                                                                 .StepContext);
            set => this._stepContext = value;
        }

        public 注入ContextSteps(ScenarioContext scenarioContext, FeatureContext featureContext)
        {
            this.ScenarioContext = scenarioContext;
            this.FeatureContext  = featureContext;

            var scenarioContextText = this.ScenarioContext.Get<string>("ScenarioContext");
            var featureContextText  = this.FeatureContext.Get<string>("FeatureContext");
            var stepContextText    = this.StepContext.Get<string>("StepContext");

            Console.WriteLine(scenarioContextText);
            Console.WriteLine(featureContextText);
            Console.WriteLine(stepContextText);
        }

        [Given(@"I also have entered (.*) into the calculator")]
        public void GivenIAlsoHaveEnteredIntoTheCalculator(int secondNumber)
        {
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