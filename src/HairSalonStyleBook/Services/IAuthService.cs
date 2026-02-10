namespace HairSalonStyleBook.Services;

/// <summary>
/// 인증 서비스 인터페이스
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// 로그인 시도. 성공하면 역할("Admin" 또는 "Viewer"), 실패하면 null 반환
    /// </summary>
    Task<string?> LoginAsync(string password);
    Task LogoutAsync();
    Task<bool> IsAuthenticatedAsync();

    /// <summary>
    /// 현재 로그인된 사용자의 역할 반환 ("Admin", "Viewer", 또는 null)
    /// </summary>
    Task<string?> GetRoleAsync();
}
