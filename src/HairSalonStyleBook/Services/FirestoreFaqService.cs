using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using HairSalonStyleBook.Models;
using Microsoft.Extensions.Configuration;

namespace HairSalonStyleBook.Services;

/// <summary>
/// FAQ Firestore 서비스 (faqs 컬렉션)
/// </summary>
public class FirestoreFaqService : IFaqService
{
    private readonly HttpClient _http;
    private readonly string _baseUrl;
    private readonly string _apiKey;
    private List<FaqItem>? _cache;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public FirestoreFaqService(HttpClient http, IConfiguration config)
    {
        _http = http;
        var projectId = config["Firebase:ProjectId"] ?? "";
        _apiKey = config["Firebase:ApiKey"] ?? "";
        _baseUrl = $"https://firestore.googleapis.com/v1/projects/{projectId}/databases/(default)/documents";
    }

    public async Task<List<FaqItem>> GetAllAsync()
    {
        if (_cache != null)
            return _cache.ToList();

        try
        {
            var response = await _http.GetAsync($"{_baseUrl}/faqs?key={_apiKey}&pageSize=100");
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"[FaqService] 목록 조회 실패: {response.StatusCode}");
                return new List<FaqItem>();
            }

            var json = await response.Content.ReadFromJsonAsync<FirestoreListResponse>(JsonOptions);
            if (json?.Documents == null)
                return new List<FaqItem>();

            _cache = json.Documents.Select(MapFromFirestore).Where(f => f != null).Select(f => f!).ToList();
            return _cache.ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[FaqService] 목록 조회 예외: {ex.Message}");
            return new List<FaqItem>();
        }
    }

    public async Task<FaqItem> CreateAsync(FaqItem item)
    {
        item.Id = Guid.NewGuid().ToString("N")[..12];
        item.CreatedAt = DateTime.UtcNow;
        item.UpdatedAt = DateTime.UtcNow;

        var doc = MapToFirestore(item);
        var content = new StringContent(JsonSerializer.Serialize(doc, JsonOptions), Encoding.UTF8, "application/json");

        var response = await _http.PostAsync($"{_baseUrl}/faqs?documentId={item.Id}&key={_apiKey}", content);
        if (!response.IsSuccessStatusCode)
            Console.WriteLine($"[FaqService] 생성 실패: {response.StatusCode} (id={item.Id})");
        response.EnsureSuccessStatusCode();
        _cache = null;
        return item;
    }

    public async Task UpdateAsync(FaqItem item)
    {
        item.UpdatedAt = DateTime.UtcNow;

        var doc = MapToFirestore(item);
        var content = new StringContent(JsonSerializer.Serialize(doc, JsonOptions), Encoding.UTF8, "application/json");

        var response = await _http.PatchAsync($"{_baseUrl}/faqs/{item.Id}?key={_apiKey}", content);
        if (!response.IsSuccessStatusCode)
            Console.WriteLine($"[FaqService] 수정 실패: {response.StatusCode} (id={item.Id})");
        response.EnsureSuccessStatusCode();
        _cache = null;
    }

    public async Task DeleteAsync(string id)
    {
        var response = await _http.DeleteAsync($"{_baseUrl}/faqs/{id}?key={_apiKey}");
        if (!response.IsSuccessStatusCode)
            Console.WriteLine($"[FaqService] 삭제 실패: {response.StatusCode} (id={id})");
        response.EnsureSuccessStatusCode();
        _cache = null;
    }

    private static FaqItem? MapFromFirestore(FirestoreDocument doc)
    {
        try
        {
            var fields = doc.Fields;
            if (fields == null) return null;

            var id = doc.Name?.Split('/').LastOrDefault() ?? "";

            return new FaqItem
            {
                Id = id,
                Title = GetStr(fields, "title"),
                Description = GetStr(fields, "description"),
                Category = GetStr(fields, "category"),
                ImageUrl = GetStr(fields, "imageUrl"),
                Order = GetInt(fields, "order"),
                IsPublished = GetBool(fields, "isPublished", true),
                CreatedAt = GetTimestamp(fields, "createdAt"),
                UpdatedAt = GetTimestamp(fields, "updatedAt"),
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[FaqService] 문서 매핑 실패: {ex.Message}");
            return null;
        }
    }

    private static FirestoreFields MapToFirestore(FaqItem item)
    {
        return new FirestoreFields
        {
            Fields = new Dictionary<string, FirestoreValue>
            {
                ["title"] = new() { StringValue = item.Title },
                ["description"] = new() { StringValue = item.Description },
                ["category"] = new() { StringValue = item.Category },
                ["imageUrl"] = new() { StringValue = item.ImageUrl },
                ["order"] = new() { IntegerValue = item.Order.ToString() },
                ["isPublished"] = new() { BooleanValue = item.IsPublished },
                ["createdAt"] = new() { TimestampValue = item.CreatedAt.ToString("o") },
                ["updatedAt"] = new() { TimestampValue = item.UpdatedAt.ToString("o") },
            }
        };
    }

    private static string GetStr(Dictionary<string, FirestoreValue> fields, string key)
        => fields.TryGetValue(key, out var v) ? v.StringValue ?? "" : "";

    private static int GetInt(Dictionary<string, FirestoreValue> fields, string key)
        => fields.TryGetValue(key, out var v) && int.TryParse(v.IntegerValue, out var n) ? n : 0;

    private static bool GetBool(Dictionary<string, FirestoreValue> fields, string key, bool fallback)
        => fields.TryGetValue(key, out var v) && v.BooleanValue.HasValue ? v.BooleanValue.Value : fallback;

    private static DateTime GetTimestamp(Dictionary<string, FirestoreValue> fields, string key)
        => fields.TryGetValue(key, out var v) && DateTime.TryParse(v.TimestampValue, out var dt) ? dt : DateTime.UtcNow;
}
