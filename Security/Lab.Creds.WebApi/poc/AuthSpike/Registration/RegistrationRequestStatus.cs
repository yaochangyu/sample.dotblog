namespace AuthSpike.Registration;

/// <summary>
/// 登錄申請（憑證與簽章金鑰共用）的狀態（12、13 單）：只有待核准的申請可被核准或拒絕，已核准或已拒絕的申請不可再改變。
/// </summary>
public enum RegistrationRequestStatus
{
    Pending,
    Approved,
    Rejected,
}

public static class RegistrationRequestStatusWire
{
    /// <summary>申請狀態的對外字串（管理介面回應）。</summary>
    public static string ToWire(this RegistrationRequestStatus status) => status switch
    {
        RegistrationRequestStatus.Pending => "pending",
        RegistrationRequestStatus.Approved => "approved",
        RegistrationRequestStatus.Rejected => "rejected",
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "未知的申請狀態"),
    };
}
