using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.JSInterop;

namespace HairSalonStyleBook.Services;

/// <summary>
/// 간단한 비밀번호 기반 인증 서비스 (Admin/Viewer 역할 지원)
/// </summary>
public class SimpleAuthService : IAuthService
{
    private readonly string _adminPasswordHash;
    private readonly string _viewerPasswordHash;
    private readonly IJSRuntime _js;
    private const string AuthKey = "salon_auth";
    private const string RoleKey = "salon_role";

    public SimpleAuthService(IConfiguration config, IJSRuntime js)
    {
        _adminPasswordHash = config["Auth:AdminPasswordHash"] ?? "";
        _viewerPasswordHash = config["Auth:ViewerPasswordHash"] ?? "";
        _js = js;
    }

    public async Task<string?> LoginAsync(string password)
    {
        var hash = ComputeSha256(password);

        // 상수 시간 비교로 타이밍 공격 방지
        string? role = null;
        if (FixedTimeEquals(hash, _adminPasswordHash))
            role = "Admin";
        else if (FixedTimeEquals(hash, _viewerPasswordHash))
            role = "Viewer";

        if (role == null)
            return null;

        await _js.InvokeVoidAsync("localStorage.setItem", AuthKey, "true");
        await _js.InvokeVoidAsync("localStorage.setItem", RoleKey, role);
        return role;
    }

    public async Task LogoutAsync()
    {
        try
        {
            // 인증 정보 명시적 삭제
            await _js.InvokeVoidAsync("localStorage.removeItem", AuthKey);
            await _js.InvokeVoidAsync("localStorage.removeItem", RoleKey);

            // 삭제 확인 후 잔여 데이터가 있으면 재삭제
            var check = await _js.InvokeAsync<string?>("localStorage.getItem", AuthKey);
            if (check != null)
            {
                await _js.InvokeVoidAsync("localStorage.removeItem", AuthKey);
                await _js.InvokeVoidAsync("localStorage.removeItem", RoleKey);
            }
        }
        catch
        {
            // JSInterop 실패 시 eval로 직접 삭제 시도
            try
            {
                await _js.InvokeVoidAsync("eval",
                    $"localStorage.removeItem('{AuthKey}'); localStorage.removeItem('{RoleKey}');");
            }
            catch { /* 무시 */ }
        }
    }

    public async Task<bool> IsAuthenticatedAsync()
    {
        try
        {
            var value = await _js.InvokeAsync<string?>("localStorage.getItem", AuthKey);
            return value == "true";
        }
        catch
        {
            return false;
        }
    }

    public async Task<string?> GetRoleAsync()
    {
        try
        {
            return await _js.InvokeAsync<string?>("localStorage.getItem", RoleKey);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>상수 시간 문자열 비교 (타이밍 공격 방지)</summary>
    private static bool FixedTimeEquals(string a, string b)
    {
        if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b))
            return false;
        var aBytes = Encoding.UTF8.GetBytes(a);
        var bBytes = Encoding.UTF8.GetBytes(b);
        return CryptographicOperations.FixedTimeEquals(aBytes, bBytes);
    }

    private static string ComputeSha256(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLower();
    }
}
