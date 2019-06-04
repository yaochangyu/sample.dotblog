using System;
using TechTalk.SpecFlow;

namespace Lab.Db.TestCase.UnitTest
{
    public abstract class BaseSteps : Steps
    {
        public abstract string StepName { get; set; }
        public string TempConnectionString =
            $@"data source=(localdb)\mssqllocaldb;initial catalog=Lab.Db.TestCase.UnitTest.{0}integrated security=True;MultipleActiveResultSets=True;App=EntityFramework";

    }
}