using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web.Http;
using Newtonsoft.Json;

namespace Tfs.WebHook.Controllers
{
    //[Authorize]
    public class MessagesController : ApiController
    {
        // GET api/messages
        public IEnumerable<string> Get()
        {
            return new[] {"value1", "value2"};
        }

        // POST api/messages
        public HttpResponseMessage Post(RootObject root)
        {
            Console.WriteLine(root.message.text);
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                //Copy paste uri from Teams
                var uri =
                    "https://outlook.office.com/webhook/c4075e61-bb4a-44c8-be88-2c25d09b6984@1c710ac2-a31a-42aa-b24f-37290a05b49f/IncomingWebhook/04c9470ae4234fedaea0e9ff834e7eb1/c54b8ab1-bd7d-4994-9c5d-c3625d67e28b";
                var msg = new TeamsHook {title = root.message.text, themeColor = "0072C6"};
                msg.text = root.message.markdown;
                var teamsMsg = new StringContent(JsonConvert.SerializeObject(msg));
                var response = client.PostAsync(uri, teamsMsg).Result;
                var responseString = response.Content.ReadAsStringAsync().Result;
                Console.WriteLine(responseString);
                return response;
            }
        }
    }
}