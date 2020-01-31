using Nancy;
using Nancy.Diagnostics;

namespace Lab.ServiceNet4
{
    public class CustomBootstrapper : DefaultNancyBootstrapper
    {
        protected override DiagnosticsConfiguration DiagnosticsConfiguration =>
            new DiagnosticsConfiguration {Password = @"pass@w0rd1~"};
    }
}