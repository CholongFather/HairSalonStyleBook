using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using HairSalonStyleBook.Models;
using Microsoft.Extensions.Configuration;
using static HairSalonStyleBook.Services.FirestoreHelper;

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
                Console.WriteLine($"[ShopConfigService] 설정 조회 실패: {response.StatusCode}");
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
                WifiName5G = GetStr(doc.Fields, "wifiName5G", ""),
                WifiName24G = GetStr(doc.Fields, "wifiName24G", ""),
                WifiPassword = GetStr(doc.Fields, "wifiPassword", ""),
                BankName = GetStr(doc.Fields, "bankName", ""),
                AccountNumber = GetStr(doc.Fields, "accountNumber", ""),
                AccountHolder = GetStr(doc.Fields, "accountHolder", ""),
                InstagramUrl = GetStr(doc.Fields, "instagramUrl", ""),
                KakaoChannelUrl = GetStr(doc.Fields, "kakaoChannelUrl", ""),
                NaverPlaceId = GetStr(doc.Fields, "naverPlaceId", ""),
                NaverSearchKeyword = GetStr(doc.Fields, "naverSearchKeyword", ""),
                KakaoPayUrl = GetStr(doc.Fields, "kakaoPayUrl", ""),
                KakaoPayEnabled = GetBool(doc.Fields, "kakaoPayEnabled", false),
                NaverPayUrl = GetStr(doc.Fields, "naverPayUrl", ""),
                NaverPayEnabled = GetBool(doc.Fields, "naverPayEnabled", false),
                SnsInstagramEnabled = GetBool(doc.Fields, "snsInstagramEnabled", true),
                SnsKakaoEnabled = GetBool(doc.Fields, "snsKakaoEnabled", true),
                SnsNaverPlaceEnabled = GetBool(doc.Fields, "snsNaverPlaceEnabled", true),
                SnsNaverReviewEnabled = GetBool(doc.Fields, "snsNaverReviewEnabled", true),
                CalendarEnabled = GetBool(doc.Fields, "calendarEnabled", false)
            };
            return _cache;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ShopConfigService] 설정 조회 예외: {ex.Message}");
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
            ["accountHolder"] = new() { StringValue = config.AccountHolder },
            ["instagramUrl"] = new() { StringValue = config.InstagramUrl ?? "" },
            ["kakaoChannelUrl"] = new() { StringValue = config.KakaoChannelUrl ?? "" },
            ["naverPlaceId"] = new() { StringValue = config.NaverPlaceId ?? "" },
            ["naverSearchKeyword"] = new() { StringValue = config.NaverSearchKeyword ?? "" },
            ["kakaoPayUrl"] = new() { StringValue = config.KakaoPayUrl ?? "" },
            ["kakaoPayEnabled"] = new() { BooleanValue = config.KakaoPayEnabled },
            ["naverPayUrl"] = new() { StringValue = config.NaverPayUrl ?? "" },
            ["naverPayEnabled"] = new() { BooleanValue = config.NaverPayEnabled },
            ["snsInstagramEnabled"] = new() { BooleanValue = config.SnsInstagramEnabled },
            ["snsKakaoEnabled"] = new() { BooleanValue = config.SnsKakaoEnabled },
            ["snsNaverPlaceEnabled"] = new() { BooleanValue = config.SnsNaverPlaceEnabled },
            ["snsNaverReviewEnabled"] = new() { BooleanValue = config.SnsNaverReviewEnabled },
            ["calendarEnabled"] = new() { BooleanValue = config.CalendarEnabled }
        };

        var body = new FirestoreFields { Fields = fields };
        var content = new StringContent(JsonSerializer.Serialize(body, JsonOptions), Encoding.UTF8, "application/json");

        var response = await _http.PatchAsync($"{_baseUrl}/config/shop?key={_apiKey}", content);
        if (!response.IsSuccessStatusCode)
            Console.WriteLine($"[ShopConfigService] 설정 저장 실패: {response.StatusCode}");
        response.EnsureSuccessStatusCode();
        _cache = config; // 캐시 즉시 갱신
    }

}
