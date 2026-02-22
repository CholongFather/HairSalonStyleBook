using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using HairSalonStyleBook.Models;
using Microsoft.Extensions.Configuration;

namespace HairSalonStyleBook.Services;

/// <summary>
/// 다꾸 캘린더 Firestore 서비스 (calendarDeco 컬렉션)
/// </summary>
public class FirestoreCalendarDecoService : ICalendarDecoService
{
    private readonly HttpClient _http;
    private readonly string _baseUrl;
    private readonly string _apiKey;
    private Dictionary<string, CalendarMonth>? _cache;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public FirestoreCalendarDecoService(HttpClient http, IConfiguration config)
    {
        _http = http;
        var projectId = config["Firebase:ProjectId"] ?? "";
        _apiKey = config["Firebase:ApiKey"] ?? "";
        _baseUrl = $"https://firestore.googleapis.com/v1/projects/{projectId}/databases/(default)/documents";
    }

    public async Task<CalendarMonth> GetMonthAsync(int year, int month)
    {
        var id = $"{year}-{month:D2}";

        // 캐시 확인
        if (_cache != null && _cache.TryGetValue(id, out var cached))
            return cached;

        try
        {
            var response = await _http.GetAsync($"{_baseUrl}/calendarDeco/{id}?key={_apiKey}");
            if (!response.IsSuccessStatusCode)
            {
                // 문서 없으면 빈 객체 반환
                return NewMonth(year, month);
            }

            var doc = await response.Content.ReadFromJsonAsync<FirestoreDocument>(JsonOptions);
            if (doc?.Fields == null)
                return NewMonth(year, month);

            var result = MapFromFirestore(doc);
            _cache ??= new();
            _cache[id] = result;
            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CalendarDecoService] 월 조회 예외: {ex.Message}");
            return NewMonth(year, month);
        }
    }

    public async Task<List<CalendarMonth>> GetPublishedMonthsAsync()
    {
        try
        {
            var queryUrl = $"{_baseUrl}:runQuery?key={_apiKey}";
            var query = new Dictionary<string, object>
            {
                ["structuredQuery"] = new Dictionary<string, object>
                {
                    ["from"] = new[] { new { collectionId = "calendarDeco" } },
                    ["where"] = new
                    {
                        fieldFilter = new
                        {
                            field = new { fieldPath = "isPublished" },
                            op = "EQUAL",
                            value = new { booleanValue = true }
                        }
                    },
                    ["orderBy"] = new[]
                    {
                        new { field = new { fieldPath = "year" }, direction = "DESCENDING" },
                        new { field = new { fieldPath = "month" }, direction = "DESCENDING" }
                    }
                }
            };

            var content = new StringContent(JsonSerializer.Serialize(query, JsonOptions), Encoding.UTF8, "application/json");
            var response = await _http.PostAsync(queryUrl, content);
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"[CalendarDecoService] 게시 목록 조회 실패: {response.StatusCode}");
                return new();
            }

            var results = await response.Content.ReadFromJsonAsync<List<FirestoreQueryResult>>(JsonOptions);
            if (results == null) return new();

            return results
                .Where(r => r.Document?.Fields != null)
                .Select(r => MapFromFirestore(r.Document!))
                .ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CalendarDecoService] 게시 목록 예외: {ex.Message}");
            return new();
        }
    }

    public async Task SaveMonthAsync(CalendarMonth data)
    {
        data.Id = $"{data.Year}-{data.Month:D2}";
        data.UpdatedAt = DateTime.UtcNow;

        var doc = MapToFirestore(data);
        var content = new StringContent(JsonSerializer.Serialize(doc, JsonOptions), Encoding.UTF8, "application/json");

        var response = await _http.PatchAsync($"{_baseUrl}/calendarDeco/{data.Id}?key={_apiKey}", content);
        if (!response.IsSuccessStatusCode)
            Console.WriteLine($"[CalendarDecoService] 저장 실패: {response.StatusCode}");
        response.EnsureSuccessStatusCode();

        // 캐시 갱신
        _cache ??= new();
        _cache[data.Id] = data;
    }

    public async Task SetPublishedAsync(string monthId, bool published)
    {
        var data = await GetMonthByIdAsync(monthId);
        data.IsPublished = published;
        data.UpdatedAt = DateTime.UtcNow;
        await SaveMonthAsync(data);
    }

    public async Task DeleteMonthAsync(string monthId)
    {
        var response = await _http.DeleteAsync($"{_baseUrl}/calendarDeco/{monthId}?key={_apiKey}");
        if (!response.IsSuccessStatusCode)
            Console.WriteLine($"[CalendarDecoService] 삭제 실패: {response.StatusCode}");
        response.EnsureSuccessStatusCode();
        _cache?.Remove(monthId);
    }

    #region Helpers

    private async Task<CalendarMonth> GetMonthByIdAsync(string monthId)
    {
        var parts = monthId.Split('-');
        if (parts.Length == 2 && int.TryParse(parts[0], out var y) && int.TryParse(parts[1], out var m))
            return await GetMonthAsync(y, m);
        return new CalendarMonth { Id = monthId };
    }

    private static CalendarMonth NewMonth(int year, int month) => new()
    {
        Id = $"{year}-{month:D2}",
        Year = year,
        Month = month,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    #endregion

    #region Firestore Mapping

    private static CalendarMonth MapFromFirestore(FirestoreDocument doc)
    {
        var fields = doc.Fields!;
        var id = doc.Name?.Split('/').LastOrDefault() ?? "";

        return new CalendarMonth
        {
            Id = id,
            Year = GetInt(fields, "year"),
            Month = GetInt(fields, "month"),
            BackgroundColor = GetStr(fields, "backgroundColor", "#fffdf7"),
            BackgroundPattern = GetStr(fields, "backgroundPattern", "none"),
            CustomTitle = GetStr(fields, "customTitle"),
            CellDecos = MapCellDecosFrom(fields),
            DDays = MapDDaysFrom(fields),
            FreeElements = MapFreeElementsFrom(fields),
            IsPublished = GetBool(fields, "isPublished"),
            CreatedAt = GetTimestamp(fields, "createdAt"),
            UpdatedAt = GetTimestamp(fields, "updatedAt"),
        };
    }

    private static FirestoreFields MapToFirestore(CalendarMonth data)
    {
        return new FirestoreFields
        {
            Fields = new Dictionary<string, FirestoreValue>
            {
                ["year"] = new() { IntegerValue = data.Year.ToString() },
                ["month"] = new() { IntegerValue = data.Month.ToString() },
                ["backgroundColor"] = new() { StringValue = data.BackgroundColor },
                ["backgroundPattern"] = new() { StringValue = data.BackgroundPattern },
                ["customTitle"] = new() { StringValue = data.CustomTitle },
                ["cellDecos"] = new()
                {
                    MapValue = new FirestoreMapValue
                    {
                        Fields = data.CellDecos.ToDictionary(
                            kv => kv.Key,
                            kv => new FirestoreValue
                            {
                                MapValue = new FirestoreMapValue
                                {
                                    Fields = MapCellDecoToFirestore(kv.Value)
                                }
                            }
                        )
                    }
                },
                ["freeElements"] = new()
                {
                    ArrayValue = new FirestoreArrayValue
                    {
                        Values = data.FreeElements.Select(e => new FirestoreValue
                        {
                            MapValue = new FirestoreMapValue
                            {
                                Fields = MapFreeElementToFirestore(e)
                            }
                        }).ToList()
                    }
                },
                ["ddays"] = new()
                {
                    MapValue = new FirestoreMapValue
                    {
                        Fields = data.DDays.ToDictionary(
                            kv => kv.Key,
                            kv => new FirestoreValue { StringValue = kv.Value }
                        )
                    }
                },
                ["isPublished"] = new() { BooleanValue = data.IsPublished },
                ["createdAt"] = new() { TimestampValue = data.CreatedAt.ToString("o") },
                ["updatedAt"] = new() { TimestampValue = data.UpdatedAt.ToString("o") },
            }
        };
    }

    // -- CellDeco 매핑 --

    private static Dictionary<string, CalendarCellDeco> MapCellDecosFrom(Dictionary<string, FirestoreValue> fields)
    {
        if (!fields.TryGetValue("cellDecos", out var v) || v.MapValue?.Fields == null)
            return new();

        var result = new Dictionary<string, CalendarCellDeco>();
        foreach (var kv in v.MapValue.Fields)
        {
            if (kv.Value.MapValue?.Fields == null) continue;
            var f = kv.Value.MapValue.Fields;
            result[kv.Key] = new CalendarCellDeco
            {
                BackgroundColor = GetStr(f, "backgroundColor"),
                Memo = GetStr(f, "memo"),
                Stickers = GetStringArray(f, "stickers"),
                TextColor = GetStr(f, "textColor"),
            };
        }
        return result;
    }

    private static Dictionary<string, FirestoreValue> MapCellDecoToFirestore(CalendarCellDeco cell) => new()
    {
        ["backgroundColor"] = new() { StringValue = cell.BackgroundColor },
        ["memo"] = new() { StringValue = cell.Memo },
        ["stickers"] = new()
        {
            ArrayValue = new FirestoreArrayValue
            {
                Values = cell.Stickers.Select(s => new FirestoreValue { StringValue = s }).ToList()
            }
        },
        ["textColor"] = new() { StringValue = cell.TextColor },
    };

    // -- DDays 매핑 --

    private static Dictionary<string, string> MapDDaysFrom(Dictionary<string, FirestoreValue> fields)
    {
        if (!fields.TryGetValue("ddays", out var v) || v.MapValue?.Fields == null)
            return new();

        return v.MapValue.Fields
            .Where(kv => !string.IsNullOrEmpty(kv.Value.StringValue))
            .ToDictionary(kv => kv.Key, kv => kv.Value.StringValue!);
    }

    // -- FreeElement 매핑 --

    private static List<CalendarFreeElement> MapFreeElementsFrom(Dictionary<string, FirestoreValue> fields)
    {
        if (!fields.TryGetValue("freeElements", out var v) || v.ArrayValue?.Values == null)
            return new();

        return v.ArrayValue.Values
            .Where(e => e.MapValue?.Fields != null)
            .Select(e =>
            {
                var f = e.MapValue!.Fields!;
                return new CalendarFreeElement
                {
                    Id = GetStr(f, "id"),
                    Type = GetStr(f, "type", "emoji"),
                    Content = GetStr(f, "content"),
                    X = GetDouble(f, "x", 50),
                    Y = GetDouble(f, "y", 50),
                    Scale = GetDouble(f, "scale", 1.0),
                    Rotation = GetDouble(f, "rotation", 0),
                    ZIndex = GetInt(f, "zIndex", 1),
                    Opacity = GetDouble(f, "opacity", 1.0),
                    Font = GetStr(f, "font", "default"),
                    Color = GetStr(f, "color", "#333333"),
                    Width = GetDouble(f, "width", 30),
                    Pattern = GetStr(f, "pattern", "stripe"),
                    IsLocked = GetBool(f, "isLocked"),
                    Frame = GetStr(f, "frame", "none"),
                };
            })
            .ToList();
    }

    private static Dictionary<string, FirestoreValue> MapFreeElementToFirestore(CalendarFreeElement e) => new()
    {
        ["id"] = new() { StringValue = e.Id },
        ["type"] = new() { StringValue = e.Type },
        ["content"] = new() { StringValue = e.Content },
        ["x"] = new() { DoubleValue = e.X },
        ["y"] = new() { DoubleValue = e.Y },
        ["scale"] = new() { DoubleValue = e.Scale },
        ["rotation"] = new() { DoubleValue = e.Rotation },
        ["zIndex"] = new() { IntegerValue = e.ZIndex.ToString() },
        ["opacity"] = new() { DoubleValue = e.Opacity },
        ["font"] = new() { StringValue = e.Font },
        ["color"] = new() { StringValue = e.Color },
        ["width"] = new() { DoubleValue = e.Width },
        ["pattern"] = new() { StringValue = e.Pattern },
        ["isLocked"] = new() { BooleanValue = e.IsLocked },
        ["frame"] = new() { StringValue = e.Frame },
    };

    // -- 유틸리티 --

    private static string GetStr(Dictionary<string, FirestoreValue> fields, string key, string fallback = "")
        => fields.TryGetValue(key, out var v) ? v.StringValue ?? fallback : fallback;

    private static bool GetBool(Dictionary<string, FirestoreValue> fields, string key, bool defaultVal = false)
        => fields.TryGetValue(key, out var v) && v.BooleanValue.HasValue ? v.BooleanValue.Value : defaultVal;

    private static double GetDouble(Dictionary<string, FirestoreValue> fields, string key, double defaultVal = 0)
        => fields.TryGetValue(key, out var v) && v.DoubleValue.HasValue ? v.DoubleValue.Value : defaultVal;

    private static int GetInt(Dictionary<string, FirestoreValue> fields, string key, int defaultVal = 0)
        => fields.TryGetValue(key, out var v) && int.TryParse(v.IntegerValue, out var n) ? n : defaultVal;

    private static DateTime GetTimestamp(Dictionary<string, FirestoreValue> fields, string key)
        => fields.TryGetValue(key, out var v) && DateTime.TryParse(v.TimestampValue, out var dt) ? dt : DateTime.UtcNow;

    private static List<string> GetStringArray(Dictionary<string, FirestoreValue> fields, string key)
    {
        if (!fields.TryGetValue(key, out var v) || v.ArrayValue?.Values == null)
            return new();
        return v.ArrayValue.Values.Where(x => x.StringValue != null).Select(x => x.StringValue!).ToList();
    }

    #endregion
}
