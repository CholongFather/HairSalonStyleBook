using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using HairSalonStyleBook.Models;
using Microsoft.Extensions.Configuration;

namespace HairSalonStyleBook.Services;

/// <summary>
/// 로그인 보안 서비스 - 기기 핑거프린트 기반 로그인 시도 기록 및 차단
/// </summary>
public class FirestoreLoginSecurityService : ILoginSecurityService
{
    private readonly HttpClient _http;
    private readonly string _baseUrl;
    private readonly string _apiKey;
    private List<string>? _blockedCache;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public FirestoreLoginSecurityService(HttpClient http, IConfiguration config)
    {
        _http = http;
        var projectId = config["Firebase:ProjectId"] ?? "";
        _apiKey = config["Firebase:ApiKey"] ?? "";
        _baseUrl = $"https://firestore.googleapis.com/v1/projects/{projectId}/databases/(default)/documents";
    }

    public async Task LogAttemptAsync(LoginAttempt attempt)
    {
        try
        {
            var id = Guid.NewGuid().ToString("N")[..12];
            var fields = new FirestoreFields
            {
                Fields = new Dictionary<string, FirestoreValue>
                {
                    ["deviceFingerprint"] = new() { StringValue = attempt.DeviceFingerprint },
                    ["deviceInfo"] = new() { StringValue = attempt.DeviceInfo },
                    ["screenSize"] = new() { StringValue = attempt.ScreenSize },
                    ["timestamp"] = new() { TimestampValue = attempt.Timestamp.ToString("o") },
                    ["success"] = new() { BooleanValue = attempt.Success }
                }
            };
            var content = new StringContent(JsonSerializer.Serialize(fields, JsonOptions), Encoding.UTF8, "application/json");
            await _http.PostAsync($"{_baseUrl}/loginAttempts?documentId={id}&key={_apiKey}", content);
        }
        catch { /* 로깅 실패 무시 */ }
    }

    public async Task<List<LoginAttempt>> GetAttemptsAsync()
    {
        try
        {
            var response = await _http.GetAsync($"{_baseUrl}/loginAttempts?key={_apiKey}&orderBy=timestamp desc&pageSize=100");
            if (!response.IsSuccessStatusCode)
                return new();

            var json = await response.Content.ReadFromJsonAsync<FirestoreListResponse>(JsonOptions);
            return json?.Documents?.Select(MapFromFirestore).Where(a => a != null).Select(a => a!).ToList() ?? new();
        }
        catch { return new(); }
    }

    public async Task<List<string>> GetBlockedDevicesAsync()
    {
        if (_blockedCache != null) return _blockedCache;

        try
        {
            var response = await _http.GetAsync($"{_baseUrl}/config/blockedDevices?key={_apiKey}");
            if (!response.IsSuccessStatusCode)
                return _blockedCache = new();

            var doc = await response.Content.ReadFromJsonAsync<FirestoreDocument>(JsonOptions);
            if (doc?.Fields != null && doc.Fields.TryGetValue("devices", out var devicesVal))
            {
                _blockedCache = devicesVal.ArrayValue?.Values?
                    .Select(v => v.StringValue ?? "")
                    .Where(s => !string.IsNullOrEmpty(s))
                    .ToList() ?? new();
                return _blockedCache;
            }
        }
        catch { }

        return _blockedCache = new();
    }

    public async Task BlockDeviceAsync(string fingerprint)
    {
        var blocked = await GetBlockedDevicesAsync();
        if (!blocked.Contains(fingerprint))
            blocked.Add(fingerprint);
        await SaveBlockedList(blocked);
    }

    public async Task UnblockDeviceAsync(string fingerprint)
    {
        var blocked = await GetBlockedDevicesAsync();
        blocked.Remove(fingerprint);
        await SaveBlockedList(blocked);
    }

    public async Task<bool> IsBlockedAsync(string fingerprint)
    {
        var blocked = await GetBlockedDevicesAsync();
        return blocked.Contains(fingerprint);
    }

    private async Task SaveBlockedList(List<string> devices)
    {
        try
        {
            var fields = new FirestoreFields
            {
                Fields = new Dictionary<string, FirestoreValue>
                {
                    ["devices"] = new()
                    {
                        ArrayValue = new FirestoreArrayValue
                        {
                            Values = devices.Select(d => new FirestoreValue { StringValue = d }).ToList()
                        }
                    }
                }
            };
            var content = new StringContent(JsonSerializer.Serialize(fields, JsonOptions), Encoding.UTF8, "application/json");
            await _http.PatchAsync($"{_baseUrl}/config/blockedDevices?key={_apiKey}", content);
            _blockedCache = devices;
        }
        catch { }
    }

    private static LoginAttempt? MapFromFirestore(FirestoreDocument doc)
    {
        try
        {
            var fields = doc.Fields;
            if (fields == null) return null;

            return new LoginAttempt
            {
                Id = doc.Name?.Split('/').LastOrDefault() ?? "",
                DeviceFingerprint = GetString(fields, "deviceFingerprint"),
                DeviceInfo = GetString(fields, "deviceInfo"),
                ScreenSize = GetString(fields, "screenSize"),
                Timestamp = GetTimestamp(fields, "timestamp"),
                Success = fields.TryGetValue("success", out var v) && v.BooleanValue == true
            };
        }
        catch { return null; }
    }

    private static string GetString(Dictionary<string, FirestoreValue> fields, string key)
        => fields.TryGetValue(key, out var v) ? v.StringValue ?? "" : "";

    private static DateTime GetTimestamp(Dictionary<string, FirestoreValue> fields, string key)
        => fields.TryGetValue(key, out var v) && DateTime.TryParse(v.TimestampValue, out var dt) ? dt : DateTime.UtcNow;
}
