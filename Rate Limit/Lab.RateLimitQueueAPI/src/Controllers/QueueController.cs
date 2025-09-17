using Lab.RateLimitQueueAPI.Models;
using Lab.RateLimitQueueAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace Lab.RateLimitQueueAPI.Controllers
{
    /// <summary>
    /// 提供與佇列互動的端點。
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class QueueController : ControllerBase
    {
        /// <summary>
        /// 佇列處理常式。
        /// </summary>
        private readonly IQueueHandler _queueHandler;
        
        /// <summary>
        /// 用於記錄日誌的記錄器。
        /// </summary>
        private readonly ILogger<QueueController> _logger;

        /// <summary>
        /// 初始化 QueueController 的新執行個體。
        /// </summary>
        /// <param name="queueHandler">佇列處理常式。</param>
        /// <param name="logger">記錄器。</param>
        public QueueController(IQueueHandler queueHandler, ILogger<QueueController> logger)
        {
            _queueHandler = queueHandler;
            _logger = logger;
        }

        /// <summary>
        /// 查詢佇列中請求的狀態。
        /// </summary>
        /// <param name="queueId">佇列項目 ID。</param>
        /// <returns>佇列狀態資訊。</returns>
        [HttpGet("{queueId}/status")]
        [ProducesResponseType(typeof(QueueStatusResponse), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<QueueStatusResponse>> GetQueueStatus(string queueId)
        {
            var status = await _queueHandler.GetQueueStatusAsync(queueId);
            
            if (status == null)
            {
                return NotFound($"找不到佇列項目 ID 為 {queueId} 的項目");
            }

            return Ok(status);
        }

        /// <summary>
        /// 取得目前佇列長度。
        /// </summary>
        /// <returns>佇列長度資訊。</returns>
        [HttpGet("length")]
        [ProducesResponseType(typeof(object), 200)]
        public async Task<ActionResult> GetQueueLength()
        {
            var length = await _queueHandler.GetQueueLengthAsync();
            var isFull = await _queueHandler.IsQueueFullAsync();

            return Ok(new { 
                QueueLength = length,
                IsFull = isFull,
                Timestamp = DateTime.UtcNow
            });
        }
    }
}

