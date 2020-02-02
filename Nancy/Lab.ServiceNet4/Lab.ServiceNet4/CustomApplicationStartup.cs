using System;
using Nancy.Bootstrapper;

namespace Lab.ServiceNet4
{
    public class CustomApplicationStartup : IApplicationStartup
    {
        public void Initialize(IPipelines pipelines)
        {
            pipelines.BeforeRequest.AddItemToEndOfPipeline(p =>
                                                           {
                                                               Console.WriteLine("OnBefore");
                                                               return null;
                                                           });
            pipelines.AfterRequest.AddItemToEndOfPipeline(p => { Console.WriteLine("OnAfter"); });
            pipelines.OnError.AddItemToEndOfPipeline((p, ex) =>
                                                     {
                                                         Console.WriteLine(ex.ToString());
                                                         return null;
                                                     });
        }
    }
}