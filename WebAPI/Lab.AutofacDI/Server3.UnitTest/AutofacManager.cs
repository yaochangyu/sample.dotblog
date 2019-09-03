using System;
using System.Reflection;
using System.Web.Http;
using Autofac;
using Autofac.Integration.WebApi;

namespace Server3.UnitTest
{
    public class AutofacManager
    {
        private readonly HttpConfiguration HttpConfig;
        private          ContainerBuilder  Builder;
        private          IContainer        Container;

        public AutofacManager(HttpConfiguration httpConfig)
        {
            this.HttpConfig = httpConfig;
        }

        public ContainerBuilder CreateApiBuilder()
        {
            this.Builder = new ContainerBuilder();
            var builder = this.Builder;
            var assembly = Assembly.Load("Server3");
            builder.RegisterApiControllers(assembly).PropertiesAutowired();

            builder.RegisterAssemblyTypes(assembly)
                   .As<IProductRepository>()
                   .SingleInstance()
                   .AsImplementedInterfaces()
                   .Keyed<IProductRepository>(k => k.Name);
            return builder;
        }

        public IContainer CreateContainer()
        {
            this.Container = this.Builder.Build();

            var container          = this.Container;
            var dependencyResolver = new AutofacWebApiDependencyResolver(container);
            this.HttpConfig.DependencyResolver = dependencyResolver;

            return container;
        }
    }
}