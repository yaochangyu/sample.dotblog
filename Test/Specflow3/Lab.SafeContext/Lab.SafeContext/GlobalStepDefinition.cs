using TechTalk.SpecFlow;

namespace Lab.SafeContext
{
    [Binding]
    public sealed class GlobalStepDefinition : Steps
    {
        private ScenarioStepContext _stepContext;

        public ScenarioStepContext StepContext
        {
            get => this._stepContext ?? (this._stepContext = this.ScenarioContext.StepContext);
            set => this._stepContext = value;
        }

        [BeforeStep]
        public void BeforeStep()
        {
            this.StepContext.Set("Global", "StepContext");
        }

        [BeforeScenario]
        public void BeforeTest()
        {
            this.ScenarioContext.Set("Global", "ScenarioContext");
            this.FeatureContext.Set("Global", "FeatureContext");
        }
    }
}