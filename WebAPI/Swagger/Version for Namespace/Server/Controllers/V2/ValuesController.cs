using System.Web.Http;

namespace Server.Controllers.V2
{
    /// <summary>
    ///     v2
    /// </summary>
    [VersionRoute("api/version", 2)]
    public class ValuesController : ApiController
    {
        /// <summary>
        /// 查詢
        /// </summary>
        /// <response code="200"/>
        /// <returns></returns>
        public IHttpActionResult Get()
        {
            return this.Ok("我是第二版");
        }
    }
}