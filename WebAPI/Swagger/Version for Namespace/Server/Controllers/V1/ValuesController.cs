using System.Web.Http;

namespace Server.Controllers.V1
{
    /// <summary>
    ///     Values控制器
    /// </summary>
    [VersionedRoute("api/version", 1)]
    public class ValuesController : ApiController
    {
        public IHttpActionResult Get()
        {
            return this.Ok("我是地板");
        }
    }
}