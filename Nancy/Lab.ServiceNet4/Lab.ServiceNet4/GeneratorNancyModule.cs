using System;
using Nancy;

namespace Lab.ServiceNet4
{
    public class GeneratorNancyModule : NancyModule
    {
        public GeneratorNancyModule()
        {
            this.Before += p =>
                           {
                               Console.WriteLine("OnBefore");
                               return null;
                           };
            this.After += p => { Console.WriteLine("OnAfter"); };
            this.OnError += (p, ex) =>
                            {
                                Console.WriteLine(ex.ToString());
                                return null;
                            };
            this.ModulePath    = "api/generator";
            this.Get["/guid"]  = p => Guid.NewGuid().ToString();
            this.Get["/error"] = p => throw new Exception("壞掉了唷");
        }
    }
}