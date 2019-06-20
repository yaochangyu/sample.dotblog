using System;
using TechTalk.SpecFlow;

namespace Lab.CallOtherStep.UnitTest
{
    [Binding]
    public class SpecFlowFeature1Steps:Steps
    {
        [Given(@"call 加法 feature")]
        public void GivenCall加法Feature()
        {
            
            foreach (var valuePair in this.TestThreadContext)
            {
                }
        }
    }
}
