using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Lab.RateLimitQueueAPI.Controllers
{
    /// <summary>
    /// 提供用於測試不同速率限制策略的端點。
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class TestController : ControllerBase
    {
        /// <summary>
        /// 用於記錄日誌的記錄器。
        /// </summary>
        private readonly ILogger<TestController> _logger;

        /// <summary>
        /// 初始化 TestController 的新執行個體。
        /// </summary>
        /// <param name="logger">記錄器。</param>
        public TestController(ILogger<TestController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// 測試端點 - 使用 FIFO 佇列機制。
        /// </summary>
        /// <returns>測試回應。</returns>
        [HttpGet("fifo")]
        [EnableRateLimiting("FifoPolicy")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(202)]
        [ProducesResponseType(429)]
        public async Task<ActionResult> TestFifo()
        {
            await Task.Delay(100); // 模擬一些處理
            
            return Ok(new { 
                Message = "FIFO 請求已成功處理",
                Timestamp = DateTime.UtcNow,
                RequestId = Guid.NewGuid().ToString()
            });
        }

        /// <summary>
        /// 測試端點 - 使用優先權佇列機制。
        /// </summary>
        /// <param name="priority">請求優先權 (0-10，數字越大優先權越高)。</param>
        /// <returns>測試回應。</returns>
        [HttpGet("priority")]
        [EnableRateLimiting("PriorityPolicy")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(202)]
        [ProducesResponseType(429)]
        public async Task<ActionResult> TestPriority([FromQuery] int priority = 0)
        {
            await Task.Delay(100); // 模擬一些處理
            
            return Ok(new { 
                Message = "優先權請求已成功處理",
                Priority = priority,
                Timestamp = DateTime.UtcNow,
                RequestId = Guid.NewGuid().ToString()
            });
        }

        /// <summary>
        /// 測試端點 - 使用自適應延遲機制。
        /// </summary>
        /// <returns>測試回應。</returns>
        [HttpGet("adaptive")]
        [EnableRateLimiting("AdaptivePolicy")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(429)]
        public async Task<ActionResult> TestAdaptive()
        {
            await Task.Delay(100); // 模擬一些處理
            
            return Ok(new { 
                Message = "自適應請求已成功處理",
                Timestamp = DateTime.UtcNow,
                RequestId = Guid.NewGuid().ToString()
            });
        }

        /// <summary>
        /// 資料處理端點。
        /// </summary>
        /// <returns>處理結果。</returns>
        [HttpPost("data")]
        [EnableRateLimiting("FifoPolicy")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(202)]
        [ProducesResponseType(429)]
        public async Task<ActionResult> ProcessData([FromBody] object data)
        {
            await Task.Delay(200); // 模擬資料處理
            
            return Ok(new { 
                Message = "資料已成功處理",
                ProcessedAt = DateTime.UtcNow,
                RequestId = Guid.NewGuid().ToString(),
                DataReceived = data != null
            });
        }

        /// <summary>
        /// 計算端點。
        /// </summary>
        /// <param name="value">要計算的值。</param>
        /// <returns>計算結果。</returns>
        [HttpGet("calculate")]
        [EnableRateLimiting("PriorityPolicy")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(202)]
        [ProducesResponseType(429)]
        public async Task<ActionResult> Calculate([FromQuery] int value = 1)
        {
            await Task.Delay(150); // 模擬計算
            
            var result = value * value + new Random().Next(1, 100);
            
            return Ok(new { 
                Input = value,
                Result = result,
                CalculatedAt = DateTime.UtcNow,
                RequestId = Guid.NewGuid().ToString()
            });
        }
    }
}

