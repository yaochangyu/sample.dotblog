using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Polly;

namespace Lab.WinFormNet48
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            this.InitializeComponent();
        }

        private static void _01_標準用法()
        {
            Policy

                // 1. 處理甚麼樣的例外
                .Handle<HttpRequestException>()

                //    或者返回條件(非必要)
                .OrResult<HttpResponseMessage>(r => r.StatusCode == HttpStatusCode.BadGateway)

                // 2. 重試策略，包含重試次數
                .Retry(3, (reponse, retryCount, context) =>
                          {
                              var result = reponse.Result;
                              if (result != null)
                              {
                                  var errorMsg = result.Content
                                                       .ReadAsStringAsync()
                                                       .GetAwaiter()
                                                       .GetResult();
                                  Console.WriteLine($"標準用法，發生錯誤：{errorMsg}，第 {retryCount} 次重試");
                              }
                              else
                              {
                                  var exception = reponse.Exception;
                                  Console.WriteLine($"標準用法，發生錯誤：{exception.Message}，第 {retryCount} 次重試");
                              }
                          })

                // 3. 執行內容
                .Execute(FakeRequest);
        }

        /// <summary>
        ///     https://github.com/App-vNext/Polly#wait-and-retry
        /// </summary>
        private static void _02_延遲重試_固定周期()
        {
            var retryPolicy = Policy.HandleResult<HttpResponseMessage>(r => r.StatusCode == HttpStatusCode.BadGateway)
                                    .WaitAndRetry(new[]
                                                  {
                                                      TimeSpan.FromSeconds(5),
                                                      TimeSpan.FromSeconds(10),
                                                      TimeSpan.FromSeconds(15)
                                                  },
                                                  (response, retryTime, context) =>
                                                  {
                                                      var errorMsg = response.Result
                                                                             .Content
                                                                             .ReadAsStringAsync()
                                                                             .GetAwaiter()
                                                                             .GetResult();
                                                      Console.WriteLine($"延遲重試，發生錯誤：{errorMsg}，延遲 {retryTime} 後重試");
                                                  });
            retryPolicy.Execute(FakeRequest);
        }

        private static void _02_延遲重試_計算週期_Jitter()
        {
            //抖動演算法
            var jitterer = new Random();

            var retryPolicy = Policy.HandleResult<HttpResponseMessage>(r => r.StatusCode == HttpStatusCode.BadGateway)
                                    .WaitAndRetry(6,
                                                  retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))
                                                                  + TimeSpan.FromMilliseconds(jitterer.Next(0, 100)),
                                                  (response, retryTime, context) =>
                                                  {
                                                      var errorMsg = response.Result
                                                                             .Content
                                                                             .ReadAsStringAsync()
                                                                             .GetAwaiter()
                                                                             .GetResult();
                                                      Console.WriteLine($"延遲重試，發生錯誤：{errorMsg}，延遲 {retryTime} 後重試");
                                                  })
                ;
            retryPolicy.Execute(FakeRequest);
        }

        private static void _02_延遲重試_計算週期_次方()
        {
            var retryPolicy = Policy.HandleResult<HttpResponseMessage>(r => r.StatusCode == HttpStatusCode.BadGateway)
                                    .WaitAndRetry(6,
                                                  retryAttempt => TimeSpan.FromSeconds(Math.Pow(6, retryAttempt)),
                                                  (response, retryTime, context) =>
                                                  {
                                                      var errorMsg = response.Result
                                                                             .Content
                                                                             .ReadAsStringAsync()
                                                                             .GetAwaiter()
                                                                             .GetResult();
                                                      Console.WriteLine($"延遲重試，發生錯誤：{errorMsg}，延遲 {retryTime} 後重試");
                                                  })
                ;
            retryPolicy.Execute(FakeRequest);

        
        }

        /// <summary>
        ///     https://github.com/App-vNext/Polly#retry-forever-until-succeeds
        /// </summary>
        private static void _03_永不放棄()
        {
            var retryPolicy = Policy.HandleResult<HttpResponseMessage>(r => r.StatusCode == HttpStatusCode.BadGateway)
                                    .RetryForever((reponse, retryCount, context) =>
                                                  {
                                                      var errorMsg = reponse.Result
                                                                            .Content
                                                                            .ReadAsStringAsync()
                                                                            .GetAwaiter()
                                                                            .GetResult();
                                                      Console.WriteLine($"永不放棄，發生錯誤：{errorMsg}，第 {retryCount} 次重試");
                                                  })
                ;
            retryPolicy.Execute(FakeRequest);
        }

        private static void _04_AdvancedCircuitBreakerAsync()
        {
            Action<DelegateResult<HttpResponseMessage>, TimeSpan> onBreak = (response, retryTime) =>
                                                                            {
                                                                                Console.WriteLine("OnBreak");
                                                                            };
            Action onReset    = () => { Console.WriteLine("OnReset"); };
            Action onHalfOpen = () => { Console.WriteLine("OnHalfOpen"); };
            var policy = Policy.HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
                               .AdvancedCircuitBreakerAsync(0.25,
                                                            TimeSpan.FromSeconds(5),
                                                            2,
                                                            TimeSpan.FromSeconds(5),
                                                            onBreak,
                                                            onReset,
                                                            onHalfOpen);
            policy.Reset();

            //policy.ExecuteAndCaptureAsync(ThrowAsync);
            var execute = policy.ExecuteAsync(ThrowAsync);
            try
            {
                var messageResponse = execute.GetAwaiter()
                                             .GetResult();
                var content = messageResponse.Content
                                             .ReadAsStringAsync()
                                             .GetAwaiter()
                                             .GetResult();
                Console.WriteLine($"content:{content}\r\nstate:{policy.CircuitState}");
            }
            catch (Exception e)
            {
                Console.WriteLine($"error:{e.Message}\r\nstate:{policy.CircuitState}");
            }
        }

        private async Task<string> _04_CircuitBreaker1()
        {
            try
            {
                //Console.WriteLine($"Circuit State: {PollyCircuitBreakerRegistry.GetCircuitBreaker(2).Circ}")
                var policy = GetCircuitBreaker(2);

                return await policy.ExecuteAsync(async () => await this.GetGoodbyeMessage());
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        private static void _04_CircuitBreakerAsync()
        {
            var policy = Policy
                         .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
                         .CircuitBreakerAsync(2, TimeSpan.FromSeconds(1000));
            Task.Factory
                .StartNew(() =>
                          {
                              while (true)
                              {
                                  policy.ExecuteAsync(ThrowAsync);

                                  SpinWait.SpinUntil(() => !true, 1000);
                              }
                          });
        }

        private static void _04_CircuitBreakerAsync1()
        {
            //var breaker = Policy.Handle<HttpRequestException>()
            //                    .CircuitBreaker(20, TimeSpan.FromMinutes(1));
            //breaker.Execute(Throw);

            Action<Exception, TimeSpan, Context> onBreak = (exception, retryTime, context) =>
                                                           {
                                                               Console.WriteLine("OnBreak");
                                                           };

            Action<Context> onReset = context => { Console.WriteLine("OnReset"); };

            Action onHalfOpen = () => { Console.WriteLine("OnHalfOpen"); };

            var policy = Policy.Handle<Exception>()
                               .CircuitBreakerAsync(5, TimeSpan.FromSeconds(3), onBreak, onReset, onHalfOpen);

            try
            {
                Console.WriteLine($"{policy.CircuitState}");
                var task = policy.ExecuteAsync(ThrowAsync);
                Console.WriteLine($"{policy.CircuitState}");

                //var response = task.GetAwaiter().GetResult();
                //var result   = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                //Console.WriteLine(result);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            //var state = policy.CircuitState;
            //policy.Reset();
            //policy.Isolate();

            //var basicCircuitBreakerPolicy = Policy.Handle<HttpRequestException>()
            //                                      .OrResult<HttpResponseMessage>(p => p.IsSuccessStatusCode == false)
            //                                      .CircuitBreaker(2, TimeSpan.FromSeconds(1),
            //                                                      (result, circuitState, arg3, arg4) =>
            //                                                      {
            //                                                          Console.WriteLine("OnBreak");
            //                                                      },
            //                                                      context =>
            //                                                      {
            //                                                          Console.WriteLine("OnReset");
            //                                                      },
            //                                                      () =>
            //                                                      {
            //                                                          Console.WriteLine("OnHalfOpen");
            //                                                      })
            //    ;
            //basicCircuitBreakerPolicy.Execute(FakeRequest);

            //policy.Execute(Throw);

            //Policy.Handle<HttpRequestException>()
            //      .OrResult<HttpResponseMessage>(p => p.StatusCode == HttpStatusCode.BadGateway)
            //      .CircuitBreaker(2,              TimeSpan.FromMinutes(2), 
            //                      (response, state, arg3, arg4) => { },
            //                      context => { }, () => {})

            //      .Execute(FakeRequest);

            //Policy.Handle<HttpRequestException>()

            //      //.OrResult<HttpResponseMessage>(p => p.StatusCode == HttpStatusCode.BadGateway)
            //      .CircuitBreaker(2,
            //                      TimeSpan.FromSeconds(10),
            //                      (reponse, retryTime, Context) =>
            //                      {

            //                          //var errorMsg = reponse.Result
            //                          //                      .Content
            //                          //                      .ReadAsStringAsync()
            //                          //                      .GetAwaiter()
            //                          //                      .GetResult();

            //                          //Console.WriteLine($"延遲重試，發生錯誤：{errorMsg}，延遲 {retryTime} 後重試");
            //                      },
            //                      context =>
            //                      {
            //                          var b = context.Count == 1;
            //                      },
            //                      () => { })
            //      .Execute(Throw)
            //    ;

            //var retryPolicy = Policy.HandleResult<HttpResponseMessage>(r => r.StatusCode == HttpStatusCode.BadGateway)
            //                        .CircuitBreaker(2, TimeSpan.FromMinutes(2), (reponse, state, retryTime, context) =>
            //                                        {

            //                                        })
            //    ;
            //breaker.Execute(FakeRequest);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //_01_標準用法();

            //_02_延遲重試_固定周期();
            //_02_延遲重試_計算週期_次方();
            //_02_延遲重試_計算週期_Jitter();

            //_03_永不放棄();
            _04_CircuitBreakerAsync();

            //_04_AdvancedCircuitBreakerAsync();
        }

        private static Task<HttpRequestException> CallRatesApi()
        {
            //call the A5PI, parse the results
            return Task.FromResult(new HttpRequestException("網路設備噴掉了"));
        }

        private static HttpResponseMessage FakeRequest()
        {
            Console.WriteLine("請求網路資源中...");
            Thread.Sleep(3000);

            throw new HttpRequestException("網路設備噴掉了");

            return new HttpResponseMessage(HttpStatusCode.BadGateway) {Content = new StringContent("網路設備燒掉了")};
        }

        private static AsyncPolicy GetCircuitBreaker(int exceptionsAllowed)
        {
            return Policy
                   .Handle<Exception>()
                   .CircuitBreakerAsync(exceptionsAllowed, TimeSpan.FromSeconds(1),
                                        (ex, t) => { Console.WriteLine("Circuit broken .. !"); },
                                        () => { Console.WriteLine("Circuit reset .. !"); });
        }

        private async Task<string> GetGoodbyeMessage()
        {
            Console.WriteLine("MessageRepository GetGoodbyeMessage running..");

            //throw new HttpRequestException("網路設備噴掉了");

            ThrowRandomException();
            return "AAA";
        }

        private static void Retry(Action action, int retryCount = 3, int waitSecond = 10)
        {
            while (true)
            {
                try
                {
                    action();
                    break;
                }
                catch
                {
                    if (--retryCount == 0)
                    {
                        throw;
                    }

                    var seconds = (int) TimeSpan.FromSeconds(waitSecond)
                                                .TotalMilliseconds;
                    Thread.Sleep(seconds);
                }
            }
        }

        private static HttpResponseMessage Throw()
        {
            Console.WriteLine("請求網路資源中...");
            Thread.Sleep(3000);
            ThrowRandomException();
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("OOOK~")
            };

            //throw new Exception("網路設備噴掉了");
        }

        private static Task<HttpResponseMessage> ThrowAsync()
        {
            Console.WriteLine("請求網路資源中...");

            //Thread.Sleep(3000);
            ThrowRandomException();
            var message = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("OOOK~")
            };
            return Task.FromResult(message);

            //throw new Exception("網路設備噴掉了");
        }

        private static void ThrowRandomException()
        {
            var random = new Random().Next(0, 10);

            if (random > 5)
            {
                Console.WriteLine("Error! Throwing Exception");
                throw new Exception("Exception in MessageRepository");
            }
        }
    }
}