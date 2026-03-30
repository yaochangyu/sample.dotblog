namespace Lab.DeviceFingerprint.WebApi.Application.DTOs;

public record LoginRequest(string Username, string Password, string Fingerprint, string? DeviceName);

public record LoginResponse(string? Token, bool RequireDeviceVerification, string? UserId, string? FingerprintHash);

public record VerifyDeviceRequest(string UserId, string FingerprintHash, string Otp, string? DeviceName, string? UserAgent);

public record VerifyDeviceResponse(string Token);
