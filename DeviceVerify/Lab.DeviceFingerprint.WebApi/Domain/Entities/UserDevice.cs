namespace Lab.DeviceFingerprint.WebApi.Domain.Entities;

public class UserDevice
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string FingerprintHash { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public bool IsVerified { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastSeenAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
}
