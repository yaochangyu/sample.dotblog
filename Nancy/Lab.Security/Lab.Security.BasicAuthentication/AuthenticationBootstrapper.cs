using Nancy;
using Nancy.Authentication.Basic;
using Nancy.Bootstrapper;
using Nancy.TinyIoc;

namespace Lab.Security.BasicAuthentication
{
    public class AuthenticationBootstrapper : DefaultNancyBootstrapper
    {
        protected override void ApplicationStartup(TinyIoCContainer container, IPipelines pipelines)
        {
            base.ApplicationStartup(container, pipelines);

            var configuration = new BasicAuthenticationConfiguration(container.Resolve<IUserValidator>(),
                                                                     "MyRealm",
                                                                     UserPromptBehaviour.NonAjax);
            pipelines.EnableBasicAuthentication(configuration);
        }
    }
}