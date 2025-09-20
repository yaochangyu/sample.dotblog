namespace Lab.QueueApi.Commands;

/// <summary>
/// 清理記錄摘要回應。
/// </summary>
public class CleanupSummaryResponse
{
    /// <summary>
    /// 總清理次數。
    /// </summary>
    public int TotalCleanupCount { get; set; }

    /// <summary>
    /// 清理記錄列表。
    /// </summary>
    public List<CleanupRecord> CleanupRecords { get; set; } = new();

    /// <summary>
    /// 最後清理時間。
    /// </summary>
    public DateTime? LastCleanupTime { get; set; }

    /// <summary>
    /// 清理記錄的最大保存數量。
    /// </summary>
    public int MaxRecordsKept { get; set; }
}