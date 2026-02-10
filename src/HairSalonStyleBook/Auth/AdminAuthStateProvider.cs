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
