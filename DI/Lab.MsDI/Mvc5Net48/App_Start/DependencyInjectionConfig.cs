using System.Reflection;
using System.Web.Http;
using System.Web.Http.Controllers;
using Microsoft.Extensions.DependencyInjection;

namespace Mvc5Net48
{
    public class DependencyInjectionConfig
    {
        public static void Register(HttpConfiguration config)
        {
            var services = ConfigureServices();

            var provider = services.BuildServiceProvider();

            var resolver = new DefaultDependencyResolver(provider);
            config.DependencyResolver = resolver;
        }

        /// <summary>
        ///     使用 MS DI 註冊
        /// </summary>
        /// <returns></returns>
        private static ServiceCollection ConfigureServices()
        {
            var services = new ServiceCollection();

            //使用 Microsoft.Extensions.DependencyInjection 註冊

            //var assemblies = new[]
            //{
            //    Assembly.Load("THS.MES.MQC2.BLL"),
            //    Assembly.Load("THS.MES.MQC2.DAL"),
            //};

            ////對應 Controller
            //services.Scan(p => p.FromAssemblies(Assembly.Load("THS.MES.MQC2.WebService"))
            //                    .AddClasses(c => c.AssignableTo<IHttpController>())
            //                    .AsSelf()
            //                    .WithScopedLifetime()
            //             );

            ////對應 BLL、DAL
            ////排除 EntityModel、ViewModel
            //var excludeNames = new[]
            //{
            //    "EntityModel",
            //    "ViewModel",

            //    //"Swagger"
            //};

            //services.Scan(scan => scan.FromAssemblies(assemblies)
            //                          .AddClasses(p => p.Where(type => IsExclude(type.Namespace, excludeNames)))

            //                          //.AsImplementedInterfaces()
            //                          .AsSelfWithInterfaces()
            //                          .WithSingletonLifetime()
            //             );

            return services;
        }

        private static bool IsExclude(string source, string[] exculdes)
        {
            var isExist = false;
            foreach (var item in exculdes)
            {
                isExist = source.Contains(item);
                if (isExist)
                {
                    break;
                }
            }

            return !isExist;
        }
    }
}