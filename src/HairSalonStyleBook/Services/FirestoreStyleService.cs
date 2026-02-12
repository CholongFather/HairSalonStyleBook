using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using HairSalonStyleBook.Models;
using Microsoft.Extensions.Configuration;

namespace HairSalonStyleBook.Services;

/// <summary>
/// Firebase Firestore REST API를 사용한 스타일 서비스 구현
/// </summary>
public class FirestoreStyleService : IStyleService
{
    private readonly HttpClient _http;
    private readonly string _baseUrl;
    private readonly string _apiKey;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    // 메모리 캐시 (정적 데이터이므로 첫 로드 후 재사용)
    private List<StylePost>? _cache;

    public FirestoreStyleService(HttpClient http, IConfiguration config)
    {
        _http = http;
        var projectId = config["Firebase:ProjectId"] ?? "";
        _apiKey = config["Firebase:ApiKey"] ?? "";
        _baseUrl = $"https://firestore.googleapis.com/v1/projects/{projectId}/databases/(default)/documents";
    }

    public async Task<List<StylePost>> GetAllAsync()
    {
        // 캐시가 있으면 바로 반환 (API 호출 없음)
        if (_cache != null)
            return _cache.ToList();

        try
        {
            var allDocs = new List<FirestoreDocument>();
            string? pageToken = null;

            // Firestore REST API 페이지네이션 (기본 100건 제한)
            do
            {
                var url = $"{_baseUrl}/styles?key={_apiKey}&pageSize=300";
                if (!string.IsNullOrEmpty(pageToken))
                    url += $"&pageToken={pageToken}";

                var response = await _http.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                    break;

                var json = await response.Content.ReadFromJsonAsync<FirestoreListResponse>(JsonOptions);
                if (json?.Documents != null)
                    allDocs.AddRange(json.Documents);

                pageToken = json?.NextPageToken;
            } while (!string.IsNullOrEmpty(pageToken));

            _cache = allDocs.Select(MapFromFirestore).Where(s => s != null).Select(s => s!).ToList();
            return _cache.ToList();
        }
        catch
        {
            return new List<StylePost>();
        }
    }

    public async Task<List<StylePost>> GetByCategoryAsync(StyleCategory category)
    {
        var all = await GetAllAsync();
        if (category == StyleCategory.전체)
            return all;
        return all.Where(s => s.Category == category).ToList();
    }

    public async Task<List<StylePost>> SearchAsync(string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
            return await GetAllAsync();

        var all = await GetAllAsync();
        var lower = keyword.ToLower();
        return all.Where(s =>
            s.Title.ToLower().Contains(lower) ||
            s.Hashtags.Any(h => h.ToLower().Contains(lower))
        ).ToList();
    }

    public async Task<StylePost?> GetByIdAsync(string id)
    {
        // 캐시에서 먼저 찾기
        if (_cache != null)
            return _cache.FirstOrDefault(s => s.Id == id);

        try
        {
            var response = await _http.GetAsync($"{_baseUrl}/styles/{id}?key={_apiKey}");
            if (!response.IsSuccessStatusCode)
                return null;

            var doc = await response.Content.ReadFromJsonAsync<FirestoreDocument>(JsonOptions);
            return doc != null ? MapFromFirestore(doc) : null;
        }
        catch
        {
            return null;
        }
    }

    public async Task<StylePost> CreateAsync(StylePost style)
    {
        style.Id = Guid.NewGuid().ToString("N")[..12];
        style.CreatedAt = DateTime.UtcNow;
        style.UpdatedAt = DateTime.UtcNow;

        var firestoreDoc = MapToFirestore(style);
        var content = new StringContent(JsonSerializer.Serialize(firestoreDoc, JsonOptions), Encoding.UTF8, "application/json");

        await _http.PostAsync($"{_baseUrl}/styles?documentId={style.Id}&key={_apiKey}", content);
        InvalidateCache();
        return style;
    }

    public async Task<StylePost> UpdateAsync(StylePost style)
    {
        style.UpdatedAt = DateTime.UtcNow;

        var firestoreDoc = MapToFirestore(style);
        var content = new StringContent(JsonSerializer.Serialize(firestoreDoc, JsonOptions), Encoding.UTF8, "application/json");

        await _http.PatchAsync($"{_baseUrl}/styles/{style.Id}?key={_apiKey}", content);
        InvalidateCache();
        return style;
    }

    public async Task DeleteAsync(string id)
    {
        await _http.DeleteAsync($"{_baseUrl}/styles/{id}?key={_apiKey}");
        InvalidateCache();
    }

    /// <summary>
    /// 캐시 무효화 (관리자 작업 후 호출)
    /// </summary>
    public void InvalidateCache() => _cache = null;

    #region Firestore 매핑

    private static StylePost? MapFromFirestore(FirestoreDocument doc)
    {
        try
        {
            var fields = doc.Fields;
            if (fields == null) return null;

            // 문서 이름에서 ID 추출 (projects/.../documents/styles/ID)
            var id = doc.Name?.Split('/').LastOrDefault() ?? "";

            return new StylePost
            {
                Id = id,
                Title = GetStringValue(fields, "title"),
                Description = GetStringValue(fields, "description"),
                Category = Enum.TryParse<StyleCategory>(GetStringValue(fields, "category"), out var cat) ? cat : StyleCategory.여성숏,
                Service = Enum.TryParse<ServiceType>(GetStringValue(fields, "service"), out var svc) ? svc : ServiceType.커트,
                ImageUrls = GetArrayValue(fields, "imageUrls"),
                Hashtags = GetArrayValue(fields, "hashtags"),
                RelatedPostIds = GetArrayValue(fields, "relatedPostIds"),
                StylingTip = GetStringValue(fields, "stylingTip"),
                TreatmentDifficulty = GetIntValue(fields, "treatmentDifficulty"),
                MaintenanceLevel = GetIntValue(fields, "maintenanceLevel"),
                Duration = GetStringValue(fields, "duration"),
                RecommendedFaceShapes = GetArrayValue(fields, "recommendedFaceShapes"),
                RecommendedHairTypes = GetArrayValue(fields, "recommendedHairTypes"),
                RecommendedFor = GetArrayValue(fields, "recommendedFor"),
                RecommendedAge = GetStringValue(fields, "recommendedAge"),
                Mood = GetStringValue(fields, "mood"),
                CreatedAt = GetTimestampValue(fields, "createdAt"),
                UpdatedAt = GetTimestampValue(fields, "updatedAt"),
                CreatedBy = GetStringValue(fields, "createdBy"),
                IsPublished = GetBoolValue(fields, "isPublished")
            };
        }
        catch
        {
            return null;
        }
    }

    private static FirestoreFields MapToFirestore(StylePost style)
    {
        return new FirestoreFields
        {
            Fields = new Dictionary<string, FirestoreValue>
            {
                ["title"] = new() { StringValue = style.Title },
                ["description"] = new() { StringValue = style.Description },
                ["category"] = new() { StringValue = style.Category.ToString() },
                ["service"] = new() { StringValue = style.Service.ToString() },
                ["imageUrls"] = new() { ArrayValue = new FirestoreArrayValue { Values = style.ImageUrls.Select(v => new FirestoreValue { StringValue = v }).ToList() } },
                ["hashtags"] = new() { ArrayValue = new FirestoreArrayValue { Values = style.Hashtags.Select(v => new FirestoreValue { StringValue = v }).ToList() } },
                ["relatedPostIds"] = new() { ArrayValue = new FirestoreArrayValue { Values = style.RelatedPostIds.Select(v => new FirestoreValue { StringValue = v }).ToList() } },
                ["stylingTip"] = new() { StringValue = style.StylingTip },
                ["treatmentDifficulty"] = new() { IntegerValue = style.TreatmentDifficulty.ToString() },
                ["maintenanceLevel"] = new() { IntegerValue = style.MaintenanceLevel.ToString() },
                ["duration"] = new() { StringValue = style.Duration },
                ["recommendedFaceShapes"] = new() { ArrayValue = new FirestoreArrayValue { Values = style.RecommendedFaceShapes.Select(v => new FirestoreValue { StringValue = v }).ToList() } },
                ["recommendedHairTypes"] = new() { ArrayValue = new FirestoreArrayValue { Values = style.RecommendedHairTypes.Select(v => new FirestoreValue { StringValue = v }).ToList() } },
                ["recommendedFor"] = new() { ArrayValue = new FirestoreArrayValue { Values = style.RecommendedFor.Select(v => new FirestoreValue { StringValue = v }).ToList() } },
                ["recommendedAge"] = new() { StringValue = style.RecommendedAge },
                ["mood"] = new() { StringValue = style.Mood },
                ["createdAt"] = new() { TimestampValue = style.CreatedAt.ToString("o") },
                ["updatedAt"] = new() { TimestampValue = style.UpdatedAt.ToString("o") },
                ["createdBy"] = new() { StringValue = style.CreatedBy },
                ["isPublished"] = new() { BooleanValue = style.IsPublished }
            }
        };
    }

    private static string GetStringValue(Dictionary<string, FirestoreValue> fields, string key)
        => fields.TryGetValue(key, out var v) ? v.StringValue ?? "" : "";

    private static int GetIntValue(Dictionary<string, FirestoreValue> fields, string key)
        => fields.TryGetValue(key, out var v) && int.TryParse(v.IntegerValue, out var n) ? n : 0;

    private static bool GetBoolValue(Dictionary<string, FirestoreValue> fields, string key)
        => fields.TryGetValue(key, out var v) && v.BooleanValue == true;

    private static DateTime GetTimestampValue(Dictionary<string, FirestoreValue> fields, string key)
        => fields.TryGetValue(key, out var v) && DateTime.TryParse(v.TimestampValue, out var dt) ? dt : DateTime.UtcNow;

    private static List<string> GetArrayValue(Dictionary<string, FirestoreValue> fields, string key)
    {
        if (!fields.TryGetValue(key, out var v) || v.ArrayValue?.Values == null)
            return new List<string>();
        return v.ArrayValue.Values.Select(x => x.StringValue ?? "").Where(x => !string.IsNullOrEmpty(x)).ToList();
    }

    #endregion
}

#region Firestore REST API DTO

public class FirestoreListResponse
{
    public List<FirestoreDocument>? Documents { get; set; }
    public string? NextPageToken { get; set; }
}

public class FirestoreDocument
{
    public string? Name { get; set; }
    public Dictionary<string, FirestoreValue>? Fields { get; set; }
}

public class FirestoreFields
{
    public Dictionary<string, FirestoreValue>? Fields { get; set; }
}

public class FirestoreValue
{
    public string? StringValue { get; set; }
    public string? IntegerValue { get; set; }
    public bool? BooleanValue { get; set; }
    public string? TimestampValue { get; set; }
    public FirestoreArrayValue? ArrayValue { get; set; }
}

public class FirestoreArrayValue
{
    public List<FirestoreValue>? Values { get; set; }
}

#endregion
