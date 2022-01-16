using System.Net;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Profiling;

namespace Lab.NETMiniProfiler.ASPNetCore5.Controllers
{
    /// <summary>
    /// Value Controller
    /// </summary>
    [Route("[controller]")]
    [ApiController]
    public class ValuesController : ControllerBase
    {
        /// <summary>
        /// Get Api
        /// </summary>
        /// <returns></returns>
        // GET api/values
        [HttpGet]
        public ActionResult<IEnumerable<string>> Get()
        {
            string url1 = string.Empty;
            string url2 = string.Empty;
            using (MiniProfiler.Current.Step("Get方法"))
            {
                using (MiniProfiler.Current.Step("准备数据"))
                {
                    using (MiniProfiler.Current.CustomTiming("SQL", "SELECT * FROM Config"))
                    {
                        // 模拟一个SQL查询
                        Thread.Sleep(500);

                        url1 = "https://www.baidu.com";
                        url2 = "https://www.sina.com.cn/";
                    }
                }


                using (MiniProfiler.Current.Step("使用从数据库中查询的数据，进行Http请求"))
                {
                    using (MiniProfiler.Current.CustomTiming("HTTP", "GET " + url1))
                    {
                        var client = new WebClient();
                        var reply = client.DownloadString(url1);
                    }

                    using (MiniProfiler.Current.CustomTiming("HTTP", "GET " + url2))
                    {
                        var client = new WebClient();
                        var reply = client.DownloadString(url2);
                    }
                }
            }
            return new string[] { "value1", "value2" };
        }

        // GET api/values/5
        [HttpGet("{id}")]
        public ActionResult<string> Get(int id)
        {
            return "value";
        }

        // POST api/values
        [HttpPost]
        public void Post([FromBody] string value)
        {
        }

        // PUT api/values/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
