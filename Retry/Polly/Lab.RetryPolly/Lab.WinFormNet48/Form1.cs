using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Polly;
using Polly.CircuitBreaker;

namespace Lab.WinFormNet48
{
    public partial class Form1 : Form
    {
        private static AsyncPolicy                                    s_asyncPolicy;
        private static AsyncCircuitBreakerPolicy<HttpResponseMessage> s_asyncCircuitBreakerPolicy;

        public Form1()
        {
            this.InitializeComponent();
            s_asyncCircuitBreakerPolicy = CreateAsyncCircuitBreakerPolicy();
            s_asyncPolicy               = CreateAsyncPolicy();
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

                              Thread.Sleep(3000);
                          })

                // 3. 執行內容
                .Execute(FailResponse);

            Console.WriteLine("標準用法，完成");
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
            retryPolicy.Execute(FailResponse);
            Console.WriteLine("延遲重試，完成");
        }

        private static void _02_延遲重試_計算週期_Jitter()
        {
            //抖動演算法
            var jitterer = new Random();

            var retryPolicy = Policy.Handle<Exception>()
                                    .OrResult<HttpResponseMessage>(r => r.StatusCode == HttpStatusCode.BadGateway)
                                    .WaitAndRetry(6,
                                                  retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))
                                                                  + TimeSpan.FromMilliseconds(jitterer.Next(0, 100)),
                                                  (response, retryTime, context) =>
                                                  {
                                                      WaitAndRetryAction(response, retryTime);
                                                  })
                ;
            try
            {
                var httpResponse = retryPolicy.Execute(RandomFailResponseOrException);
                var content = httpResponse.Content
                                          .ReadAsStringAsync()
                                          .GetAwaiter()
                                          .GetResult()
                    ;
                Console.WriteLine(content);
                Console.WriteLine("延遲重試，完成");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
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
            retryPolicy.Execute(FailResponse);
            Console.WriteLine("延遲重試，完成");
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
            retryPolicy.Execute(FailResponse);
            Console.WriteLine("永不放棄，完成");
        }

        private static void _04_斷路器()
        {
            var state = s_asyncCircuitBreakerPolicy.CircuitState;
            try
            {
                Console.WriteLine($"呼叫任務前的狀態:{state}");

                var response = s_asyncCircuitBreakerPolicy.ExecuteAsync(RandomFailResponseOrExceptionAsync)
                                                          .GetAwaiter()
                                                          .GetResult();
                var content = response.Content;
                if (content != null)
                {
                    var result = content.ReadAsStringAsync().GetAwaiter().GetResult();
                    Console.WriteLine($"取得服務內容:{result}\r\n"
                                      + $"斷路器狀態:{state}");
                    if (response.IsSuccessStatusCode)
                    {
                        Console.WriteLine("斷路器，正常完成");
                    }
                }
            }
            catch (Exception e)
            {
                //state = s_asyncCircuitBreakerPolicy.CircuitState;
                Console.WriteLine($"錯誤:{e.Message}\r\n"
                                  + $"斷路器狀態:{state}");
            }
            finally
            {
                Console.WriteLine("");
            }
        }

        private static void _04_斷路器_只處理例外()
        {
            //var response = s_circuitBreakerPolicy.ExecuteAsync(RandomFailResponseOrExceptionAsync).GetAwaiter().GetResult();
            try
            {
                //var state = s_asyncPolicy..CircuitState;
                //Console.WriteLine($"呼叫任務前的狀態:{state}");
                var response = s_asyncPolicy.ExecuteAsync(ThrowExceptionAsync).GetAwaiter().GetResult();
            }
            catch (Exception e)
            {
                Console.WriteLine($"錯誤:{e.Message}"

                                  //+ $"斷路器狀態:{state}"
                                 )
                    ;
            }
            finally
            {
                Console.WriteLine("斷路器，完成");
            }

            //try
            //{
            //    var response = s_circuitBreakerPolicy.ExecuteAsync(CreateRandomRequestAsync)
            //                                         .GetAwaiter()
            //                                         .GetResult();
            //    //var content = response.Content;
            //    //if (content != null)
            //    //{
            //    //    var result = content.ReadAsStringAsync().GetAwaiter().GetResult();
            //    //    Console.WriteLine(result);
            //    //}
            //}
            //catch (Exception e)
            //{
            //    Console.WriteLine(e.Message);
            //}
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //_01_標準用法();

            //_02_延遲重試_固定周期();

            //_02_延遲重試_計算週期_次方();
            //_02_延遲重試_計算週期_Jitter();

            //_03_永不放棄();
            _04_斷路器();
        }

        private static AsyncCircuitBreakerPolicy<HttpResponseMessage> CreateAsyncCircuitBreakerPolicy()
        {
            Action<DelegateResult<HttpResponseMessage>, CircuitState, TimeSpan, Context> onBreak =
                    (response, state, retryTime, context) =>
                    {
                        var    ex  = response.Exception;
                        string msg = null;
                        if (ex != null)
                        {
                            msg = $"錯誤:{ex.Message}\r\n"
                                  + $"超過失敗上限了，先等等，過了 {retryTime} 再過來\r\n"
                                  + $"斷路器狀態:{state}"
                                ;
                        }
                        else
                        {
                            var content = response.Result
                                                  .Content
                                                  .ReadAsStringAsync()
                                                  .GetAwaiter()
                                                  .GetResult();
                            msg = $"錯誤:{content}\r\n"
                                  + $"超過失敗上限了，先等等，過了 {retryTime} 再過來\r\n"
                                  + $"斷路器狀態:{state}"
                                ;
                        }

                        Console.WriteLine(msg);
                        Console.WriteLine();
                    }
                ;
            Action<Context> onReset = context =>
                                      {
                                          var state = s_asyncCircuitBreakerPolicy.CircuitState;
                                          Console.WriteLine($"斷路器狀態:{state}");
                                      };

            Action onHalfOpen = () =>
                                {
                                    var state = s_asyncCircuitBreakerPolicy.CircuitState;
                                    Console.WriteLine($"斷路器狀態:{state}");
                                };

            var policy = Policy.Handle<Exception>()
                               .OrResult<HttpResponseMessage>(p => p.IsSuccessStatusCode == false)
                               .CircuitBreakerAsync(2, TimeSpan.FromSeconds(30), onBreak, onReset, onHalfOpen);

            return policy;
        }

        private static AsyncPolicy CreateAsyncPolicy()
        {
            Action<Exception, CircuitState, TimeSpan, Context> onBreak =
                (exception, state, retryTime, context) =>
                {
                    var msg = $"錯誤:{exception.Message}\r\n" +
                              "超過失敗上限了，先等等，"                +
                              $"過了 {retryTime} 再過來\r\n"     +
                              $"狀態:{state}";
                    Console.WriteLine(msg);
                };

            Action<Context> onReset = context => { Console.WriteLine("OnReset"); };

            Action onHalfOpen = () => { Console.WriteLine("OnHalfOpen"); };

            var policy = Policy.Handle<Exception>()
                               .CircuitBreakerAsync(2, TimeSpan.FromSeconds(10), onBreak, onReset, onHalfOpen);

            return policy;
        }

        private static HttpResponseMessage FailResponse()
        {
            Console.WriteLine("請求網路資源中...");

            //Thread.Sleep(3000);

            //throw new HttpRequestException("網路設備噴掉了");

            return new HttpResponseMessage(HttpStatusCode.BadGateway) {Content = new StringContent("網路設備燒掉了")};
        }

        private static Task<HttpResponseMessage> FailResponseAsync()
        {
            var response = FailResponse();
            return Task.FromResult(response);
        }

        private static HttpResponseMessage RandomFailResponseOrException()
        {
            Console.WriteLine("請求網路資源中...");

            var random = new Random().Next(0, 10);

            if (random <= 1)
            {
                throw new HttpRequestException("請求出現未知異常~");
            }

            var response = new HttpResponseMessage();
            if (random > 2 & random <= 6)
            {
                response.StatusCode = HttpStatusCode.OK;
                response.Content    = new StringContent("對了，媽，我在這裡~!");
            }
            else if (random > 6)
            {
                response.StatusCode = HttpStatusCode.BadGateway;
                response.Content    = new StringContent("網路設備噴掉了啦!!!");
            }

            return response;
        }

        private static Task<HttpResponseMessage> RandomFailResponseOrExceptionAsync()
        {
            var response = RandomFailResponseOrException();
            return Task.FromResult(response);
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

        private static Task<HttpResponseMessage> ThrowExceptionAsync()
        {
            throw new HttpRequestException("請求出現未知異常~");

            //var response = new HttpResponseMessage();
            //return Task.FromResult(response);
        }

        private static void WaitAndRetryAction(DelegateResult<HttpResponseMessage> response, TimeSpan retryTime)
        {
            var ex = response.Exception;
            if (ex != null)
            {
                Console.WriteLine($"重試，發生錯誤：{ex.Message}，延遲 {retryTime} 後重試");
                return;
            }

            var result = response.Result;
            if (result != null)
            {
                var errorMsg = result.Content
                                     .ReadAsStringAsync()
                                     .GetAwaiter()
                                     .GetResult();
                Console.WriteLine($"重試，發生錯誤：{errorMsg}，延遲 {retryTime} 後重試");
            }
        }
    }
}