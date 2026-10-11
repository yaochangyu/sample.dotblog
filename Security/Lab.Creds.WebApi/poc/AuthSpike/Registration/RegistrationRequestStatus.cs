namespace AuthSpike.Registration;

/// <summary>
/// 登錄申請（憑證與簽章金鑰共用）的狀態（12、13 單）：只有待核准的申請可被核准或拒絕，已核准或已拒絕的申請不可再改變。
/// </summary>
public enum RegistrationRequestStatus
{
    Pending,
    Approved,
    Rejected,
    /// <summary>待核准申請逾期（lab 暫定 24 小時，待使用者確認）；由 <see cref="RegistrationRequestStatusWire.Effective"/> 判定，不寫入儲存。</summary>
    Expired,
}

public static class RegistrationRequestStatusWire
{
    /// <summary>申請狀態的對外字串（管理介面回應）。</summary>
    public static string ToWire(this RegistrationRequestStatus status) => status switch
    {
        RegistrationRequestStatus.Pending => "pending",
        RegistrationRequestStatus.Approved => "approved",
        RegistrationRequestStatus.Rejected => "rejected",
        RegistrationRequestStatus.Expired => "expired",
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "未知的申請狀態"),
    };

    /// <summary>
    /// 申請目前的狀態：待核准的申請到達逾期時間（含）即視為已過期。已核准或已拒絕的申請不會因時間改變。
    /// 逾期判定以注入的時間為準，不寫入儲存，因此管理員查詢與核准、拒絕、上限計數都看到同一判定。
    /// </summary>
    public static RegistrationRequestStatus Effective(this RegistrationRequestStatus stored, DateTimeOffset expiresAt, DateTimeOffset now)
        => stored == RegistrationRequestStatus.Pending && now >= expiresAt ? RegistrationRequestStatus.Expired : stored;
}
