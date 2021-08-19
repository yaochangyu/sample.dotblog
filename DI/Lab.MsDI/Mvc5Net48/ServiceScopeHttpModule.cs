using System;
using System.Web;
using Microsoft.Extensions.DependencyInjection;

namespace Mvc5Net48
{
    internal class ServiceScopeHttpModule : IHttpModule
    {
        private static ServiceProvider s_serviceProvider;

        public static void SetServiceProvider(IServiceProvider serviceProvider)
        {
            s_serviceProvider = serviceProvider as ServiceProvider;
        }

        public void Dispose()
        {
            s_serviceProvider?.Dispose();
        }

        public void Init(HttpApplication context)
        {
            context.BeginRequest += this.Context_BeginRequest;
            context.EndRequest   += this.Context_EndRequest;
        }

        private void Context_BeginRequest(object sender, EventArgs e)
        {
            var context = ((HttpApplication) sender).Context;
            context.Items[typeof(IServiceScope)] = s_serviceProvider.CreateScope();
        }

        private void Context_EndRequest(object sender, EventArgs e)
        {
            var context = ((HttpApplication) sender).Context;
            if (context.Items[typeof(IServiceScope)] is IServiceScope scope)
            {
                scope.Dispose();
            }
        }
    }
}