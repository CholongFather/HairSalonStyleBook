using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using HairSalonStyleBook.Models;
using Microsoft.Extensions.Configuration;

namespace HairSalonStyleBook.Services;

/// <summary>
/// 매장 설정 Firestore 서비스 (메모리 캐시 적용)
/// </summary>
public class FirestoreShopConfigService : IShopConfigService
{
    private readonly HttpClient _http;
    private readonly string _baseUrl;
    private readonly string _apiKey;
    private ShopConfig? _cache;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public FirestoreShopConfigService(HttpClient http, IConfiguration config)
    {
        _http = http;
        var projectId = config["Firebase:ProjectId"] ?? "";
        _apiKey = config["Firebase:ApiKey"] ?? "";
        _baseUrl = $"https://firestore.googleapis.com/v1/projects/{projectId}/databases/(default)/documents";
    }

    public async Task<ShopConfig> GetAsync()
    {
        if (_cache != null)
            return _cache;

        try
        {
            var response = await _http.GetAsync($"{_baseUrl}/config/shop?key={_apiKey}");
            if (!response.IsSuccessStatusCode)
            {
                _cache = new ShopConfig(); // 기본값 사용
                return _cache;
            }

            var doc = await response.Content.ReadFromJsonAsync<FirestoreDocument>(JsonOptions);
            if (doc?.Fields == null)
            {
                _cache = new ShopConfig();
                return _cache;
            }

            _cache = new ShopConfig
            {
                WifiName5G = GetStr(doc.Fields, "wifiName5G", "U+NetAFC8_5G"),
                WifiName24G = GetStr(doc.Fields, "wifiName24G", "U+NetAFC8"),
                WifiPassword = GetStr(doc.Fields, "wifiPassword", "DE01K7#0E5"),
                BankName = GetStr(doc.Fields, "bankName", "우리은행"),
                AccountNumber = GetStr(doc.Fields, "accountNumber", "-"),
                AccountHolder = GetStr(doc.Fields, "accountHolder", "정*경")
            };
            return _cache;
        }
        catch
        {
            _cache = new ShopConfig();
            return _cache;
        }
    }

    public async Task SaveAsync(ShopConfig config)
    {
        var fields = new Dictionary<string, FirestoreValue>
        {
            ["wifiName5G"] = new() { StringValue = config.WifiName5G },
            ["wifiName24G"] = new() { StringValue = config.WifiName24G },
            ["wifiPassword"] = new() { StringValue = config.WifiPassword },
            ["bankName"] = new() { StringValue = config.BankName },
            ["accountNumber"] = new() { StringValue = config.AccountNumber },
            ["accountHolder"] = new() { StringValue = config.AccountHolder }
        };

        var body = new FirestoreFields { Fields = fields };
        var content = new StringContent(JsonSerializer.Serialize(body, JsonOptions), Encoding.UTF8, "application/json");

        await _http.PatchAsync($"{_baseUrl}/config/shop?key={_apiKey}", content);
        _cache = config; // 캐시 즉시 갱신
    }

    private static string GetStr(Dictionary<string, FirestoreValue> fields, string key, string fallback)
        => fields.TryGetValue(key, out var v) && !string.IsNullOrEmpty(v.StringValue) ? v.StringValue : fallback;
}
