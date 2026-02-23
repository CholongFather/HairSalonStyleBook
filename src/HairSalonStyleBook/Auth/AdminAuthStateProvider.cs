using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using HairSalonStyleBook.Services;

namespace HairSalonStyleBook.Auth;

/// <summary>
/// 인증 상태 관리 (Admin/Viewer 역할 지원)
/// </summary>
public class AdminAuthStateProvider : AuthenticationStateProvider
{
    private readonly IAuthService _authService;

    public AdminAuthStateProvider(IAuthService authService)
    {
        _authService = authService;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        // JSInterop이 초기 로드 시 준비되지 않을 수 있으므로 재시도
        for (int retry = 0; retry < 3; retry++)
        {
            try
            {
                var isAuthenticated = await _authService.IsAuthenticatedAsync();

                if (isAuthenticated)
                {
                    var role = await _authService.GetRoleAsync() ?? "Viewer";
                    var displayName = role == "Admin" ? "관리자" : "뷰어";

                    var claims = new[]
                    {
                        new Claim(ClaimTypes.Name, displayName),
                        new Claim(ClaimTypes.Role, role)
                    };
                    var identity = new ClaimsIdentity(claims, "SalonAuth");
                    return new AuthenticationState(new ClaimsPrincipal(identity));
                }

                // JSInterop 성공했지만 인증 안 됨 → 재시도 불필요
                break;
            }
            catch (InvalidOperationException)
            {
                // JSInterop 아직 준비 안 됨 → 잠시 대기 후 재시도
                if (retry < 2)
                    await Task.Delay(150);
            }
        }

        return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
    }

    /// <summary>
    /// 인증 상태 변경 알림
    /// </summary>
    public void NotifyAuthStateChanged()
    {
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }
}
