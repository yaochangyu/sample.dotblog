using System;
using TechTalk.SpecFlow;

namespace Lab.Db.TestCase.UnitTest
{
    public abstract class BaseSteps : Steps
    {
        public static string ConnectionString { get; set; }


        static BaseSteps()
        {
            Console.WriteLine("static Constructor");
        }

        protected BaseSteps()
        {
            Console.WriteLine("public Constructor");
        }


        [BeforeTestRun]
        public static void BeforeTestRun()
        {
            Console.WriteLine("static void BeforeTestRun");
        }

        [AfterTestRun]
        public static void AfterTestRun()
        {
            Console.WriteLine("static void AfterTestRun");
        }

        [Before]
        public static void Before()
        {
            Console.WriteLine("Base Before Scenario");
        }


    }
}