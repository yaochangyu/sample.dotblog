using System;
using Nancy;

namespace Lab.ServiceNet4
{
    public class BaseModule : NancyModule
    {
        public BaseModule()
        {
            this.Before += p =>
                           {
                               Console.WriteLine("OnBefore");
                               return null;
                           };
            this.After += p =>
                          {
                              Console.WriteLine("OnAfter");
                          };
            this.OnError += (p, ex) =>
                            {
                                Console.WriteLine(ex.ToString());
                                return null;
                            };
        }
    }
}