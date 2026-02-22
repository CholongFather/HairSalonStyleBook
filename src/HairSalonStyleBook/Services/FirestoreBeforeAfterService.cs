using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using HairSalonStyleBook.Models;
using Microsoft.Extensions.Configuration;

namespace HairSalonStyleBook.Services;

/// <summary>
/// Before/After Firestore 서비스 (beforeAfters 컬렉션)
/// </summary>
public class FirestoreBeforeAfterService : IBeforeAfterService
{
    private readonly HttpClient _http;
    private readonly string _baseUrl;
    private readonly string _apiKey;
    private List<BeforeAfterItem>? _cache;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public FirestoreBeforeAfterService(HttpClient http, IConfiguration config)
    {
        _http = http;
        var projectId = config["Firebase:ProjectId"] ?? "";
        _apiKey = config["Firebase:ApiKey"] ?? "";
        _baseUrl = $"https://firestore.googleapis.com/v1/projects/{projectId}/databases/(default)/documents";
    }

    public async Task<List<BeforeAfterItem>> GetAllAsync()
    {
        if (_cache != null)
            return _cache.ToList();

        try
        {
            var all = new List<BeforeAfterItem>();
            string? pageToken = null;

            do
            {
                var url = $"{_baseUrl}/beforeAfters?key={_apiKey}&pageSize=100";
                if (!string.IsNullOrEmpty(pageToken))
                    url += $"&pageToken={pageToken}";

                var response = await _http.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[BeforeAfterService] 목록 조회 실패: {response.StatusCode}");
                    break;
                }

                var json = await response.Content.ReadFromJsonAsync<FirestoreListResponse>(JsonOptions);
                if (json?.Documents != null)
                    all.AddRange(json.Documents.Select(MapFromFirestore).Where(x => x != null).Select(x => x!));

                pageToken = json?.NextPageToken;
            } while (!string.IsNullOrEmpty(pageToken));

            _cache = all;
            return _cache.ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[BeforeAfterService] 목록 조회 예외: {ex.Message}");
            return new List<BeforeAfterItem>();
        }
    }

    public async Task<(List<BeforeAfterItem> Items, bool HasMore)> GetPageAsync(int limit, DateTime? beforeDate = null, bool publishedOnly = false)
    {
        try
        {
            var queryUrl = $"{_baseUrl}:runQuery?key={_apiKey}";

            var orderBy = new List<object>
            {
                new { field = new { fieldPath = "createdAt" }, direction = "DESCENDING" }
            };

            // where 필터 조립
            var filters = new List<object>();

            if (publishedOnly)
            {
                filters.Add(new
                {
                    fieldFilter = new
                    {
                        field = new { fieldPath = "isPublished" },
                        op = "EQUAL",
                        value = new { booleanValue = true }
                    }
                });
            }

            if (beforeDate.HasValue)
            {
                filters.Add(new
                {
                    fieldFilter = new
                    {
                        field = new { fieldPath = "createdAt" },
                        op = "LESS_THAN",
                        value = new { timestampValue = beforeDate.Value.ToString("o") }
                    }
                });
            }

            object? where = filters.Count switch
            {
                0 => null,
                1 => filters[0],
                _ => new { compositeFilter = new { op = "AND", filters } }
            };

            var query = new Dictionary<string, object>
            {
                ["structuredQuery"] = new Dictionary<string, object>
                {
                    ["from"] = new[] { new { collectionId = "beforeAfters" } },
                    ["orderBy"] = orderBy,
                    ["limit"] = limit + 1
                }
            };

            if (where != null)
                ((Dictionary<string, object>)query["structuredQuery"])["where"] = where;

            var content = new StringContent(JsonSerializer.Serialize(query, JsonOptions), Encoding.UTF8, "application/json");
            var response = await _http.PostAsync(queryUrl, content);
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"[BeforeAfterService] 페이지 조회 실패: {response.StatusCode}");
                return (new List<BeforeAfterItem>(), false);
            }

            var results = await response.Content.ReadFromJsonAsync<List<FirestoreQueryResult>>(JsonOptions);
            if (results == null)
                return (new List<BeforeAfterItem>(), false);

            var items = results
                .Where(r => r.Document != null)
                .Select(r => MapFromFirestore(r.Document!))
                .Where(x => x != null)
                .Select(x => x!)
                .ToList();

            var hasMore = items.Count > limit;
            if (hasMore)
                items = items.Take(limit).ToList();

            return (items, hasMore);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[BeforeAfterService] 페이지 조회 예외: {ex.Message}");
            return (new List<BeforeAfterItem>(), false);
        }
    }

    public async Task<BeforeAfterItem> CreateAsync(BeforeAfterItem item)
    {
        item.Id = Guid.NewGuid().ToString("N")[..12];
        item.CreatedAt = DateTime.UtcNow;
        item.UpdatedAt = DateTime.UtcNow;

        var doc = MapToFirestore(item);
        var content = new StringContent(JsonSerializer.Serialize(doc, JsonOptions), Encoding.UTF8, "application/json");

        var response = await _http.PostAsync($"{_baseUrl}/beforeAfters?documentId={item.Id}&key={_apiKey}", content);
        if (!response.IsSuccessStatusCode)
            Console.WriteLine($"[BeforeAfterService] 생성 실패: {response.StatusCode} (id={item.Id})");
        response.EnsureSuccessStatusCode();
        _cache = null;
        return item;
    }

    public async Task UpdateAsync(BeforeAfterItem item)
    {
        item.UpdatedAt = DateTime.UtcNow;

        var doc = MapToFirestore(item);
        var content = new StringContent(JsonSerializer.Serialize(doc, JsonOptions), Encoding.UTF8, "application/json");

        var response = await _http.PatchAsync($"{_baseUrl}/beforeAfters/{item.Id}?key={_apiKey}", content);
        if (!response.IsSuccessStatusCode)
            Console.WriteLine($"[BeforeAfterService] 수정 실패: {response.StatusCode} (id={item.Id})");
        response.EnsureSuccessStatusCode();
        _cache = null;
    }

    public async Task DeleteAsync(string id)
    {
        var response = await _http.DeleteAsync($"{_baseUrl}/beforeAfters/{id}?key={_apiKey}");
        if (!response.IsSuccessStatusCode)
            Console.WriteLine($"[BeforeAfterService] 삭제 실패: {response.StatusCode} (id={id})");
        response.EnsureSuccessStatusCode();
        _cache = null;
    }

    #region Firestore Mapping

    private static BeforeAfterItem? MapFromFirestore(FirestoreDocument doc)
    {
        try
        {
            var fields = doc.Fields;
            if (fields == null) return null;

            var id = doc.Name?.Split('/').LastOrDefault() ?? "";

            return new BeforeAfterItem
            {
                Id = id,
                BeforeImageUrls = GetStringArray(fields, "beforeImageUrls"),
                AfterImageUrls = GetStringArray(fields, "afterImageUrls"),
                Title = GetStr(fields, "title"),
                Description = GetStr(fields, "description"),
                Category = GetStr(fields, "category"),
                Hashtags = GetStringArray(fields, "hashtags"),
                IsPublished = GetBool(fields, "isPublished"),
                CreatedAt = GetTimestamp(fields, "createdAt"),
                UpdatedAt = GetTimestamp(fields, "updatedAt"),
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[BeforeAfterService] 문서 매핑 실패: {ex.Message}");
            return null;
        }
    }

    private static FirestoreFields MapToFirestore(BeforeAfterItem item)
    {
        return new FirestoreFields
        {
            Fields = new Dictionary<string, FirestoreValue>
            {
                ["beforeImageUrls"] = new()
                {
                    ArrayValue = new FirestoreArrayValue
                    {
                        Values = item.BeforeImageUrls.Select(u => new FirestoreValue { StringValue = u }).ToList()
                    }
                },
                ["afterImageUrls"] = new()
                {
                    ArrayValue = new FirestoreArrayValue
                    {
                        Values = item.AfterImageUrls.Select(u => new FirestoreValue { StringValue = u }).ToList()
                    }
                },
                ["title"] = new() { StringValue = item.Title },
                ["description"] = new() { StringValue = item.Description },
                ["category"] = new() { StringValue = item.Category },
                ["hashtags"] = new()
                {
                    ArrayValue = new FirestoreArrayValue
                    {
                        Values = item.Hashtags.Select(h => new FirestoreValue { StringValue = h }).ToList()
                    }
                },
                ["isPublished"] = new() { BooleanValue = item.IsPublished },
                ["createdAt"] = new() { TimestampValue = item.CreatedAt.ToString("o") },
                ["updatedAt"] = new() { TimestampValue = item.UpdatedAt.ToString("o") },
            }
        };
    }

    private static string GetStr(Dictionary<string, FirestoreValue> fields, string key)
        => fields.TryGetValue(key, out var v) ? v.StringValue ?? "" : "";

    private static bool GetBool(Dictionary<string, FirestoreValue> fields, string key, bool defaultVal = false)
        => fields.TryGetValue(key, out var v) && v.BooleanValue.HasValue ? v.BooleanValue.Value : defaultVal;

    private static DateTime GetTimestamp(Dictionary<string, FirestoreValue> fields, string key)
        => fields.TryGetValue(key, out var v) && DateTime.TryParse(v.TimestampValue, out var dt) ? dt : DateTime.UtcNow;

    private static List<string> GetStringArray(Dictionary<string, FirestoreValue> fields, string key)
    {
        if (!fields.TryGetValue(key, out var v) || v.ArrayValue?.Values == null)
            return new List<string>();
        return v.ArrayValue.Values.Where(x => x.StringValue != null).Select(x => x.StringValue!).ToList();
    }

    #endregion
}
