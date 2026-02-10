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

        string? role = null;
        if (string.Equals(hash, _adminPasswordHash, StringComparison.OrdinalIgnoreCase))
            role = "Admin";
        else if (string.Equals(hash, _viewerPasswordHash, StringComparison.OrdinalIgnoreCase))
            role = "Viewer";

        if (role == null)
            return null;

        await _js.InvokeVoidAsync("localStorage.setItem", AuthKey, "true");
        await _js.InvokeVoidAsync("localStorage.setItem", RoleKey, role);
        return role;
    }

    public async Task LogoutAsync()
    {
        await _js.InvokeVoidAsync("localStorage.removeItem", AuthKey);
        await _js.InvokeVoidAsync("localStorage.removeItem", RoleKey);
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

    private static string ComputeSha256(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLower();
    }
}
