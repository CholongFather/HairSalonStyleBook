using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using HairSalonStyleBook.Models;
using Microsoft.Extensions.Configuration;
using static HairSalonStyleBook.Services.FirestoreHelper;

namespace HairSalonStyleBook.Services;

/// <summary>
/// 월별 매출 Firestore 서비스 (monthlyRevenues 컬렉션)
/// </summary>
public class FirestoreRevenueService : IRevenueService
{
    private readonly HttpClient _http;
    private readonly string _baseUrl;
    private readonly string _apiKey;
    private List<MonthlyRevenue>? _cache;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public FirestoreRevenueService(HttpClient http, IConfiguration config)
    {
        _http = http;
        var projectId = config["Firebase:ProjectId"] ?? "";
        _apiKey = config["Firebase:ApiKey"] ?? "";
        _baseUrl = $"https://firestore.googleapis.com/v1/projects/{projectId}/databases/(default)/documents";
    }

    public async Task<List<MonthlyRevenue>> GetAllAsync() => await GetAllCachedAsync();

    public async Task<List<MonthlyRevenue>> GetByYearAsync(int year)
    {
        var all = await GetAllCachedAsync();
        return all.Where(r => r.Year == year).OrderBy(r => r.Month).ToList();
    }

    public async Task<MonthlyRevenue?> GetAsync(int year, int month)
    {
        var all = await GetAllCachedAsync();
        return all.FirstOrDefault(r => r.Year == year && r.Month == month);
    }

    public async Task<MonthlyRevenue> SaveAsync(MonthlyRevenue item)
    {
        item.Id = $"{item.Year}-{item.Month:D2}";
        item.UpdatedAt = DateTime.UtcNow;

        // 기존 데이터 있는지 확인 → 없으면 CreatedAt 설정
        var existing = await GetAsync(item.Year, item.Month);
        if (existing == null)
            item.CreatedAt = DateTime.UtcNow;

        var doc = MapToFirestore(item);
        var json = JsonSerializer.Serialize(doc, JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // PATCH = upsert (문서 없으면 생성, 있으면 업데이트)
        var response = await _http.PatchAsync(
            $"{_baseUrl}/monthlyRevenues/{item.Id}?key={_apiKey}", content);

        if (!response.IsSuccessStatusCode)
            Console.WriteLine($"[RevenueService] 저장 실패: {response.StatusCode} (id={item.Id})");
        response.EnsureSuccessStatusCode();

        InvalidateCache();
        return item;
    }

    public async Task DeleteAsync(string id)
    {
        var response = await _http.DeleteAsync($"{_baseUrl}/monthlyRevenues/{id}?key={_apiKey}");
        if (!response.IsSuccessStatusCode)
            Console.WriteLine($"[RevenueService] 삭제 실패: {response.StatusCode} (id={id})");
        response.EnsureSuccessStatusCode();

        InvalidateCache();
    }

    public void InvalidateCache() => _cache = null;

    // --- 내부 ---

    private async Task<List<MonthlyRevenue>> GetAllCachedAsync()
    {
        if (_cache != null)
            return _cache;

        try
        {
            var response = await _http.GetAsync($"{_baseUrl}/monthlyRevenues?key={_apiKey}&pageSize=300");
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"[RevenueService] 목록 조회 실패: {response.StatusCode}");
                return new List<MonthlyRevenue>();
            }

            var result = await response.Content.ReadFromJsonAsync<FirestoreListResponse>(JsonOptions);
            if (result?.Documents == null)
                return new List<MonthlyRevenue>();

            _cache = result.Documents
                .Select(MapFromFirestore)
                .Where(r => r != null)
                .Select(r => r!)
                .ToList();
            return _cache;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RevenueService] 목록 조회 예외: {ex.Message}");
            return new List<MonthlyRevenue>();
        }
    }

    private static MonthlyRevenue? MapFromFirestore(FirestoreDocument doc)
    {
        try
        {
            var fields = doc.Fields;
            if (fields == null) return null;

            var id = doc.Name?.Split('/').LastOrDefault() ?? "";

            return new MonthlyRevenue
            {
                Id = id,
                Year = GetInt(fields, "year"),
                Month = GetInt(fields, "month"),
                CardAmount = GetLong(fields, "cardAmount"),
                CashAmount = GetLong(fields, "cashAmount"),
                Memo = GetStr(fields, "memo"),
                CreatedAt = GetTimestamp(fields, "createdAt"),
                UpdatedAt = GetTimestamp(fields, "updatedAt"),
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RevenueService] 문서 매핑 실패: {ex.Message}");
            return null;
        }
    }

    private static FirestoreFields MapToFirestore(MonthlyRevenue item)
    {
        return new FirestoreFields
        {
            Fields = new Dictionary<string, FirestoreValue>
            {
                ["year"] = new() { IntegerValue = item.Year.ToString() },
                ["month"] = new() { IntegerValue = item.Month.ToString() },
                ["cardAmount"] = new() { IntegerValue = item.CardAmount.ToString() },
                ["cashAmount"] = new() { IntegerValue = item.CashAmount.ToString() },
                ["memo"] = new() { StringValue = item.Memo },
                ["createdAt"] = new() { TimestampValue = item.CreatedAt.ToString("o") },
                ["updatedAt"] = new() { TimestampValue = item.UpdatedAt.ToString("o") },
            }
        };
    }
}
