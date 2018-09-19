using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Tfs.WebHook.Controllers
{
    //[Authorize]
    //[RoutePrefix("api/messages")]
    public class MessagesController : ApiController
    {
        private static readonly HttpClient s_httpClient;

        static MessagesController()
        {
            if (s_httpClient == null)
            {
                s_httpClient = new HttpClient();
                s_httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                var uri = AppSetting.Teams.IncomingUrl;

                //暖機
                s_httpClient.SendAsync(new HttpRequestMessage
                {
                    Method = new HttpMethod("HEAD"),
                    RequestUri = new Uri(uri + "/")
                })
                            .Result.EnsureSuccessStatusCode();
            }
        }

        // GET api/messages
        public IEnumerable<string> Get()
        {
            return new[] { "value1", "value2" };
        }

        //[Route("api/messages")]
        public HttpResponseMessage Post(TfsRootObject fromTfs)
        {
            try
            {
                var uri = AppSetting.Teams.IncomingUrl;
                var toTeams = new TeamsRootObject
                {
                    title = fromTfs.message.text,
                    themeColor = "0072C6"
                };
                toTeams.text = fromTfs.message.markdown;

                var teamsMsg = new StringContent(JsonConvert.SerializeObject(toTeams));
                var response = s_httpClient.PostAsync(uri, teamsMsg).Result;
                return response;
            }
            catch (Exception e)
            {
                throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent(e.Message)
                });
            }
        }

        [Route("api/messages/p2")]
        public HttpResponseMessage Post(HttpRequestMessage request)
        {
            try
            {
                var requestContent = request.Content.ReadAsStringAsync().Result;

                JObject jObject = JsonConvert.DeserializeObject<JObject>(requestContent);
                var publisherId = jObject.Properties().Where(p => p.Name.Contains("publisherId")).FirstOrDefault();
                //TODO:未完成
                var value = publisherId.First().Value<string>();

                var uri = AppSetting.Teams.IncomingUrl;
                var toTeams = new TeamsRootObject
                {
                    title = "demo",
                    themeColor = "0072C6"
                };
                toTeams.text = "demo";

                var teamsMsg = new StringContent(JsonConvert.SerializeObject(toTeams));
                var response = s_httpClient.PostAsync(uri, teamsMsg).Result;
                return response;
            }
            catch (Exception e)
            {
                throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent(e.Message)
                });
            }
        }
    }
}