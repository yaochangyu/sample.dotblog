using System.Web.Http;

namespace Server.Controllers.V2
{
    /// <summary>
    ///     Values控制器
    /// </summary>
    [VersionRoute("api/version", 2)]
    public class ValuesController : ApiController
    {
        public IHttpActionResult Get()
        {
            return this.Ok("我是第二版");
        }
    }
}