using System;
using Nancy;
using Nancy.Bootstrapper;
using Nancy.Diagnostics;
using Nancy.TinyIoc;

namespace Lab.ServiceNet4
{
    public class CustomBootstrapper : DefaultNancyBootstrapper
    {
        protected override DiagnosticsConfiguration DiagnosticsConfiguration =>
            new DiagnosticsConfiguration {Password = @"pass@w0rd1~"};

        protected override void RequestStartup(TinyIoCContainer container, IPipelines pipelines, NancyContext context)
        {
            base.RequestStartup(container, pipelines, context);
            //pipelines.BeforeRequest += p =>
            //                           {
            //                               Console.WriteLine("OnBefore");

            //                               return null;
            //                           };
            //pipelines.AfterRequest += p => { Console.WriteLine("OnAfter"); };
            //pipelines.OnError += (p, ex) =>
            //                     {
            //                         Console.WriteLine(ex.ToString());
            //                         return null;
            //                     };
        }
    }
}