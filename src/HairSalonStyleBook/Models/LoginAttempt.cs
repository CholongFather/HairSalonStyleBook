namespace HairSalonStyleBook.Models;

/// <summary>
/// 로그인 시도 기록
/// </summary>
public class LoginAttempt
{
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 기기 핑거프린트 (SHA256 해시)
    /// </summary>
    public string DeviceFingerprint { get; set; } = string.Empty;

    /// <summary>
    /// 기기 정보 요약 (User-Agent 기반)
    /// </summary>
    public string DeviceInfo { get; set; } = string.Empty;

    /// <summary>
    /// 화면 해상도
    /// </summary>
    public string ScreenSize { get; set; } = string.Empty;

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 로그인 성공 여부
    /// </summary>
    public bool Success { get; set; }
}
