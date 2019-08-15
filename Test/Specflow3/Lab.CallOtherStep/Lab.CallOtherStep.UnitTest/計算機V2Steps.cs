using System.Collections.Generic;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TechTalk.SpecFlow;
using TechTalk.SpecFlow.Assist;

namespace Lab.CallOtherStep.UnitTest
{
    [Binding]
    [Scope(Feature = "計算機V2")]
    public class 計算機V2Steps : Steps
    {
        [Given(@"I have entered two number into the calculator")]
        public void GivenIHaveEnteredTwoNumberIntoTheCalculator(Table table)
        {
            this.ScenarioContext.Set(table.CreateInstance<TwoVariable>(), "variable");
        }
        int          executedTests    = 0;
        List<string> scenarioNames    = new List<string>();
        List<string> scenarioStatuses = new List<string>();

        public void CaptureScenarioInformation()
        {
            
           var scenarioName = ScenarioContext.Current.ScenarioInfo.Title;
           foreach (var keyValuePair in TechTalk.SpecFlow.ScenarioContext.Current)
           {
           }

           PropertyInfo pInfo  = typeof(ScenarioContext).GetProperty("TestStatus", BindingFlags.Instance | BindingFlags.NonPublic);
           var pInfos = typeof(FeatureContext).GetProperties();
            MethodInfo   getter = pInfo.GetGetMethod(nonPublic: true);
          var  scenarioStatus = getter.Invoke(ScenarioContext.Current, null).ToString();
            scenarioNames.Add(scenarioName);
            scenarioStatuses.Add(scenarioStatus);
            executedTests++;
        }
        [Given(@"I press add and the result should be success")]
        public void GivenIPressAddAndTheResultShouldBeSuccess()
        {
            CaptureScenarioInformation();
            var featureContext = this.FeatureContext;
            var container      = featureContext.FeatureContainer;

            var enumerator = featureContext.GetEnumerator();
            while (enumerator.MoveNext())
            {
                
            }
            var      expected = 120d;
            string[] header   = {"FirstNumber", "SecondNumber"};
            string[] row      = {"50", "70"};
            var      table    = new Table(header);
            table.AddRow(row);

            this.Given(@"I have entered two number into the calculator", table);
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
            var variable    = this.ScenarioContext.Get<TwoVariable>("variable");
            var calculation = new Calculation();
            var actual      = calculation.Add(variable.FirstNumber, variable.SecondNumber);
            this.ScenarioContext.Set(actual, "actual");
        }
    }
}