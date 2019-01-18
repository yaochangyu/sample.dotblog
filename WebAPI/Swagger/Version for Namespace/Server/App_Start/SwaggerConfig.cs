using System.Web.Http;
using WebActivatorEx;
using Server;
using Swashbuckle.Application;

[assembly: PreApplicationStartMethod(typeof(SwaggerConfig), "Register")]

namespace Server
{
    public class SwaggerConfig
    {
        public static void Register()
        {
            var thisAssembly = typeof(SwaggerConfig).Assembly;

            GlobalConfiguration.Configuration
                .EnableSwagger(c =>
                    {
                        c.MultipleApiVersions((apiDesc, targetApiVersion) => SwaggerVersionHelper.ResolveVersionSupportByRouteConstraint(apiDesc, targetApiVersion),
                                              (vc) =>
                                              {
                                                  vc.Version("v2", "第二版");
                                                  vc.Version("v1", "第一版");
                                              });
                        c.CustomProvider((defaultProvider) => new CachingSwaggerProvider(defaultProvider));
                    })
                .EnableSwaggerUi(c =>
                    {
                    });
        }
    }
}
