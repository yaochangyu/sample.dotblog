using System.Collections.Concurrent;
using System.Net.Http;
using System.Text.Json;
using AuthSpike.Hosting;
using AuthSpike.Tests.Steps;

namespace AuthSpike.Tests.Support;

/// <summary>
/// 待核准申請的情境清理（19 單）：預設執行環境在所有情境間共用，未核准的申請會佔用 Client 的待核准額度，
/// 使後續情境的上限判斷受先前情境影響。每個情境結束時，把本情境於目前執行環境受理的申請以管理員拒絕，
/// 讓後續情境從零待核准開始。拒絕只改變申請狀態，不改變信任名單；已核准的申請拒絕時回 409，忽略即可。
/// </summary>
public static class PendingRequestCleanup
{
    private static readonly ConcurrentQueue<(SpikeRuntime Runtime, string CollectionPath, string RequestId)> Accepted = new();

    /// <summary>記錄一筆已受理的申請；collectionPath 為 client-certificate-requests 或 signing-key-requests。</summary>
    internal static void Track(SpikeRuntime runtime, string collectionPath, string responseBody)
    {
        using var document = JsonDocument.Parse(responseBody);
        Accepted.Enqueue((runtime, collectionPath, document.RootElement.GetProperty("requestId").GetString()!));
    }

    /// <summary>拒絕目前執行環境上殘留的申請；其他（已換用或已釋放）執行環境的申請隨該環境一起消失，直接丟棄。</summary>
    public static async Task RejectLeftoversAsync()
    {
        var current = SpikeEnvironment.Runtime;
        while (Accepted.TryDequeue(out var entry))
        {
            if (!ReferenceEquals(entry.Runtime, current))
            {
                continue;
            }

            await AdministratorInterfaceSteps.SendAdminAsync(current.AdministratorCertificate, HttpMethod.Post, $"admin/{entry.CollectionPath}/{entry.RequestId}/reject");
        }
    }
}
