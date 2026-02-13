using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using HairSalonStyleBook.Models;
using Microsoft.Extensions.Configuration;

namespace HairSalonStyleBook.Services;

/// <summary>
/// 갤러리 Firestore 서비스 (gallery 컬렉션)
/// </summary>
public class FirestoreGalleryService : IGalleryService
{
    private readonly HttpClient _http;
    private readonly string _baseUrl;
    private readonly string _apiKey;
    private List<GalleryItem>? _cache;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public FirestoreGalleryService(HttpClient http, IConfiguration config)
    {
        _http = http;
        var projectId = config["Firebase:ProjectId"] ?? "";
        _apiKey = config["Firebase:ApiKey"] ?? "";
        _baseUrl = $"https://firestore.googleapis.com/v1/projects/{projectId}/databases/(default)/documents";
    }

    public async Task<List<GalleryItem>> GetAllAsync()
    {
        if (_cache != null)
            return _cache.ToList();

        try
        {
            var all = new List<GalleryItem>();
            string? pageToken = null;

            do
            {
                var url = $"{_baseUrl}/gallery?key={_apiKey}&pageSize=100";
                if (!string.IsNullOrEmpty(pageToken))
                    url += $"&pageToken={pageToken}";

                var response = await _http.GetAsync(url);
                if (!response.IsSuccessStatusCode) break;

                var json = await response.Content.ReadFromJsonAsync<FirestoreListResponse>(JsonOptions);
                if (json?.Documents != null)
                    all.AddRange(json.Documents.Select(MapFromFirestore).Where(g => g != null).Select(g => g!));

                pageToken = json?.NextPageToken;
            } while (!string.IsNullOrEmpty(pageToken));

            _cache = all;
            return _cache.ToList();
        }
        catch
        {
            return new List<GalleryItem>();
        }
    }

    public async Task<GalleryItem> CreateAsync(GalleryItem item)
    {
        item.Id = Guid.NewGuid().ToString("N")[..12];
        item.CreatedAt = DateTime.UtcNow;
        item.UpdatedAt = DateTime.UtcNow;

        var doc = MapToFirestore(item);
        var content = new StringContent(JsonSerializer.Serialize(doc, JsonOptions), Encoding.UTF8, "application/json");

        await _http.PostAsync($"{_baseUrl}/gallery?documentId={item.Id}&key={_apiKey}", content);
        _cache = null;
        return item;
    }

    public async Task UpdateAsync(GalleryItem item)
    {
        item.UpdatedAt = DateTime.UtcNow;

        var doc = MapToFirestore(item);
        var content = new StringContent(JsonSerializer.Serialize(doc, JsonOptions), Encoding.UTF8, "application/json");

        await _http.PatchAsync($"{_baseUrl}/gallery/{item.Id}?key={_apiKey}", content);
        _cache = null;
    }

    public async Task DeleteAsync(string id)
    {
        await _http.DeleteAsync($"{_baseUrl}/gallery/{id}?key={_apiKey}");
        _cache = null;
    }

    #region Firestore Mapping

    private static GalleryItem? MapFromFirestore(FirestoreDocument doc)
    {
        try
        {
            var fields = doc.Fields;
            if (fields == null) return null;

            var id = doc.Name?.Split('/').LastOrDefault() ?? "";

            return new GalleryItem
            {
                Id = id,
                ImageUrl = GetStr(fields, "imageUrl"),
                Description = GetStr(fields, "description"),
                Hashtags = GetStringArray(fields, "hashtags"),
                Decoration = MapDecorationFrom(fields),
                History = MapHistoryFrom(fields),
                VisitDate = GetNullableTimestamp(fields, "visitDate"),
                IsPublished = GetBool(fields, "isPublished", true),
                IsLocked = GetBool(fields, "isLocked", false),
                CreatedAt = GetTimestamp(fields, "createdAt"),
                UpdatedAt = GetTimestamp(fields, "updatedAt"),
            };
        }
        catch
        {
            return null;
        }
    }

    private static FirestoreFields MapToFirestore(GalleryItem item)
    {
        return new FirestoreFields
        {
            Fields = new Dictionary<string, FirestoreValue>
            {
                ["imageUrl"] = new() { StringValue = item.ImageUrl },
                ["description"] = new() { StringValue = item.Description },
                ["hashtags"] = new()
                {
                    ArrayValue = new FirestoreArrayValue
                    {
                        Values = item.Hashtags.Select(h => new FirestoreValue { StringValue = h }).ToList()
                    }
                },
                ["decoration"] = new()
                {
                    MapValue = new FirestoreMapValue
                    {
                        Fields = MapDecoFieldsToFirestore(item.Decoration)
                    }
                },
                ["history"] = new()
                {
                    ArrayValue = new FirestoreArrayValue
                    {
                        Values = item.History.Select(h => new FirestoreValue
                        {
                            MapValue = new FirestoreMapValue
                            {
                                Fields = new Dictionary<string, FirestoreValue>(MapDecoFieldsToFirestore(h.Decoration))
                                {
                                    ["timestamp"] = new() { TimestampValue = h.Timestamp.ToString("o") },
                                }
                            }
                        }).ToList()
                    }
                },
                ["visitDate"] = item.VisitDate.HasValue
                    ? new() { TimestampValue = item.VisitDate.Value.ToString("o") }
                    : new() { NullValue = "NULL_VALUE" },
                ["isPublished"] = new() { BooleanValue = item.IsPublished },
                ["isLocked"] = new() { BooleanValue = item.IsLocked },
                ["createdAt"] = new() { TimestampValue = item.CreatedAt.ToString("o") },
                ["updatedAt"] = new() { TimestampValue = item.UpdatedAt.ToString("o") },
            }
        };
    }

    private static GalleryDecoration MapDecorationFrom(Dictionary<string, FirestoreValue> fields)
    {
        if (!fields.TryGetValue("decoration", out var v) || v.MapValue?.Fields == null)
            return new GalleryDecoration();

        var f = v.MapValue.Fields;
        return MapDecoFieldsFrom(f);
    }

    private static List<DecorationHistory> MapHistoryFrom(Dictionary<string, FirestoreValue> fields)
    {
        if (!fields.TryGetValue("history", out var v) || v.ArrayValue?.Values == null)
            return new List<DecorationHistory>();

        return v.ArrayValue.Values
            .Where(h => h.MapValue?.Fields != null)
            .Select(h =>
            {
                var f = h.MapValue!.Fields!;
                return new DecorationHistory
                {
                    Timestamp = GetTimestamp(f, "timestamp"),
                    Decoration = MapDecoFieldsFrom(f)
                };
            })
            .ToList();
    }

    /// <summary>Decoration 필드를 Firestore dict으로 변환</summary>
    private static Dictionary<string, FirestoreValue> MapDecoFieldsToFirestore(GalleryDecoration d) => new()
    {
        ["frameType"] = new() { StringValue = d.FrameType },
        ["textContent"] = new() { StringValue = d.TextContent },
        ["textFont"] = new() { StringValue = d.TextFont },
        ["textPosition"] = new() { StringValue = d.TextPosition },
        ["textColor"] = new() { StringValue = d.TextColor },
        ["textX"] = new() { DoubleValue = d.TextX },
        ["textY"] = new() { DoubleValue = d.TextY },
        ["textScale"] = new() { DoubleValue = d.TextScale },
        ["sticker"] = new() { StringValue = d.Sticker },
        ["stickerPosition"] = new() { StringValue = d.StickerPosition },
        ["stickerX"] = new() { DoubleValue = d.StickerX },
        ["stickerY"] = new() { DoubleValue = d.StickerY },
        ["stickerScale"] = new() { DoubleValue = d.StickerScale },
    };

    /// <summary>Firestore dict에서 Decoration 읽기</summary>
    private static GalleryDecoration MapDecoFieldsFrom(Dictionary<string, FirestoreValue> f) => new()
    {
        FrameType = GetStr(f, "frameType"),
        TextContent = GetStr(f, "textContent"),
        TextFont = GetStr(f, "textFont"),
        TextPosition = GetStr(f, "textPosition"),
        TextColor = GetStr(f, "textColor"),
        TextX = GetDouble(f, "textX", 50),
        TextY = GetDouble(f, "textY", 85),
        TextScale = GetDouble(f, "textScale", 1.0),
        Sticker = GetStr(f, "sticker"),
        StickerPosition = GetStr(f, "stickerPosition") is { Length: > 0 } sp ? sp : "bottom-right",
        StickerX = GetDouble(f, "stickerX", 80),
        StickerY = GetDouble(f, "stickerY", 80),
        StickerScale = GetDouble(f, "stickerScale", 1.0),
    };

    private static string GetStr(Dictionary<string, FirestoreValue> fields, string key)
        => fields.TryGetValue(key, out var v) ? v.StringValue ?? "" : "";

    private static bool GetBool(Dictionary<string, FirestoreValue> fields, string key, bool defaultVal = false)
        => fields.TryGetValue(key, out var v) && v.BooleanValue.HasValue ? v.BooleanValue.Value : defaultVal;

    private static double GetDouble(Dictionary<string, FirestoreValue> fields, string key, double defaultVal = 0)
        => fields.TryGetValue(key, out var v) && v.DoubleValue.HasValue ? v.DoubleValue.Value : defaultVal;

    private static DateTime GetTimestamp(Dictionary<string, FirestoreValue> fields, string key)
        => fields.TryGetValue(key, out var v) && DateTime.TryParse(v.TimestampValue, out var dt) ? dt : DateTime.UtcNow;

    private static DateTime? GetNullableTimestamp(Dictionary<string, FirestoreValue> fields, string key)
        => fields.TryGetValue(key, out var v) && DateTime.TryParse(v.TimestampValue, out var dt) ? dt : null;

    private static List<string> GetStringArray(Dictionary<string, FirestoreValue> fields, string key)
    {
        if (!fields.TryGetValue(key, out var v) || v.ArrayValue?.Values == null)
            return new List<string>();
        return v.ArrayValue.Values.Where(x => x.StringValue != null).Select(x => x.StringValue!).ToList();
    }

    #endregion
}
