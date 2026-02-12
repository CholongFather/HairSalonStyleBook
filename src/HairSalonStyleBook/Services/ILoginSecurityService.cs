using HairSalonStyleBook.Models;

namespace HairSalonStyleBook.Services;

/// <summary>
/// 로그인 보안 서비스 인터페이스
/// </summary>
public interface ILoginSecurityService
{
    Task LogAttemptAsync(LoginAttempt attempt);
    Task<List<LoginAttempt>> GetAttemptsAsync();
    Task<List<string>> GetBlockedDevicesAsync();
    Task BlockDeviceAsync(string fingerprint);
    Task UnblockDeviceAsync(string fingerprint);
    Task<bool> IsBlockedAsync(string fingerprint);
}
