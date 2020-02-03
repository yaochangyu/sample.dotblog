using System.Linq;
using Nancy;
using Nancy.Extensions;

namespace Lab.Security.BasicAuthentication
{
    public static class ModuleSecurityExtension
    {
        public static void RequiresAuthentication(this INancyModule module, string[] excludes)
        {
            module.AddBeforeHookOrExecute(p =>
                                          {
                                              Response response = null;
                                              if (excludes.Contains(p.ResolvedRoute.Description.Name))
                                              {
                                                  return response;
                                              }

                                              if (p.CurrentUser == null ||
                                                  string.IsNullOrWhiteSpace(p.CurrentUser.UserName))
                                              {
                                                  response = new Response
                                                  {
                                                      StatusCode = HttpStatusCode.Unauthorized
                                                  };
                                              }

                                              return response;
                                          }, "Requires Authentication");
        }
    }
}