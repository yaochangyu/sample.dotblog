using Lab.DeviceFingerprint.WebApi.Application.DTOs;

namespace Lab.DeviceFingerprint.WebApi.Application.Services;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request, string userAgent, CancellationToken ct = default);

    Task<VerifyDeviceResponse> VerifyDeviceAsync(VerifyDeviceRequest request, CancellationToken ct = default);
}
