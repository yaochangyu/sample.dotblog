using System.Reflection;
using System.Web.Http;
using System.Web.Http.Controllers;
using Autofac;
using Autofac.Core;
using Autofac.Integration.WebApi;
using Server.UnitTest.Controllers;

namespace Server.UnitTest
{
    public class AutofacManager
    {
        private readonly HttpConfiguration HttpConfig;
        private          ContainerBuilder  Builder;

        private IContainer Container;

        public AutofacManager(HttpConfiguration httpConfig)
        {
            this.HttpConfig = httpConfig;
        }

        public ContainerBuilder CreateApiBuilder()
        {
            this.Builder = new ContainerBuilder();
            var builder = this.Builder;

            var assembly = Assembly.GetExecutingAssembly();
            builder.RegisterApiControllers(assembly);

            builder.RegisterAssemblyTypes(assembly)
                   .As<IProductRepository>()
                   .AsImplementedInterfaces()
                   .Keyed<IProductRepository>(k => k.Name);
            return builder;
        }

        public IContainer CreateContainer(ContainerBuilder builder)
        {
            this.Container = builder.Build();

            var container          = this.Container;

                     var dependencyResolver = new AutofacWebApiDependencyResolver(container);
            this.HttpConfig.DependencyResolver = dependencyResolver;

            return container;
        }

        public T GetController<T>() where T : ApiController
        {
            var container  = this.Container;
            var controller = (T) container.ResolveService(new TypedService(typeof(T)));
            return controller;
        }

        public void SetController<T>(T controller) where T : ApiController
        {
            this.Builder.RegisterInstance(controller);
        }
    }
}