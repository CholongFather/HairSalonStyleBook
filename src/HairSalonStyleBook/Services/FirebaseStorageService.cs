using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace HairSalonStyleBook.Services;

/// <summary>
/// Firebase Storage REST API를 사용한 이미지 서비스
/// </summary>
public class FirebaseStorageService : IImageService
{
    private readonly HttpClient _http;
    private readonly string _bucket;

    public FirebaseStorageService(HttpClient http, IConfiguration config)
    {
        _http = http;
        var projectId = config["Firebase:ProjectId"] ?? "";
        _bucket = config["Firebase:StorageBucket"] ?? $"{projectId}.firebasestorage.app";
    }

    public Task<string> UploadAsync(string fileName, byte[] data, string contentType)
        => UploadAsync(fileName, data, contentType, "styles");

    public async Task<string> UploadAsync(string fileName, byte[] data, string contentType, string folder)
    {
        var storagePath = $"{folder}/{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}_{fileName}";
        var encodedPath = Uri.EscapeDataString(storagePath);

        var uploadUrl = $"https://firebasestorage.googleapis.com/v0/b/{_bucket}/o?uploadType=media&name={encodedPath}";

        var content = new ByteArrayContent(data);
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);

        var response = await _http.PostAsync(uploadUrl, content);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var token = json.GetProperty("downloadTokens").GetString();

        // 다운로드 URL 구성
        return $"https://firebasestorage.googleapis.com/v0/b/{_bucket}/o/{encodedPath}?alt=media&token={token}";
    }

    public async Task DeleteAsync(string imageUrl)
    {
        try
        {
            // URL에서 storage path 추출
            var uri = new Uri(imageUrl);
            var path = uri.AbsolutePath;
            var oIndex = path.IndexOf("/o/", StringComparison.Ordinal);
            if (oIndex < 0) return;

            var encodedPath = path[(oIndex + 3)..];

            var deleteUrl = $"https://firebasestorage.googleapis.com/v0/b/{_bucket}/o/{encodedPath}";
            await _http.DeleteAsync(deleteUrl);
        }
        catch
        {
            // 삭제 실패는 무시
        }
    }
}
