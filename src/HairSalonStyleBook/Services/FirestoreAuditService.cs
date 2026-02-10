using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using HairSalonStyleBook.Models;
using Microsoft.Extensions.Configuration;

namespace HairSalonStyleBook.Services;

/// <summary>
/// Firebase Firestore 감사 로그 서비스
/// </summary>
public class FirestoreAuditService : IAuditService
{
    private readonly HttpClient _http;
    private readonly string _baseUrl;
    private readonly string _apiKey;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public FirestoreAuditService(HttpClient http, IConfiguration config)
    {
        _http = http;
        var projectId = config["Firebase:ProjectId"] ?? "";
        _apiKey = config["Firebase:ApiKey"] ?? "";
        _baseUrl = $"https://firestore.googleapis.com/v1/projects/{projectId}/databases/(default)/documents";
    }

    public async Task<List<AuditLog>> GetAllAsync()
    {
        try
        {
            var response = await _http.GetAsync($"{_baseUrl}/auditLogs?key={_apiKey}&orderBy=timestamp desc");
            if (!response.IsSuccessStatusCode)
                return new List<AuditLog>();

            var json = await response.Content.ReadFromJsonAsync<FirestoreListResponse>(JsonOptions);
            return json?.Documents?.Select(MapFromFirestore).Where(a => a != null).Select(a => a!).ToList()
                   ?? new List<AuditLog>();
        }
        catch
        {
            return new List<AuditLog>();
        }
    }

    public async Task LogAsync(string action, string targetId, string targetTitle, string details)
    {
        var log = new AuditLog
        {
            Id = Guid.NewGuid().ToString("N")[..12],
            Action = action,
            TargetId = targetId,
            TargetTitle = targetTitle,
            Details = details,
            Timestamp = DateTime.UtcNow
        };

        var firestoreDoc = MapToFirestore(log);
        var content = new StringContent(JsonSerializer.Serialize(firestoreDoc, JsonOptions), Encoding.UTF8, "application/json");

        await _http.PostAsync($"{_baseUrl}/auditLogs?documentId={log.Id}&key={_apiKey}", content);
    }

    private static AuditLog? MapFromFirestore(FirestoreDocument doc)
    {
        try
        {
            var fields = doc.Fields;
            if (fields == null) return null;

            var id = doc.Name?.Split('/').LastOrDefault() ?? "";

            return new AuditLog
            {
                Id = id,
                Action = GetString(fields, "action"),
                TargetId = GetString(fields, "targetId"),
                TargetTitle = GetString(fields, "targetTitle"),
                Details = GetString(fields, "details"),
                Timestamp = GetTimestamp(fields, "timestamp")
            };
        }
        catch
        {
            return null;
        }
    }

    private static FirestoreFields MapToFirestore(AuditLog log)
    {
        return new FirestoreFields
        {
            Fields = new Dictionary<string, FirestoreValue>
            {
                ["action"] = new() { StringValue = log.Action },
                ["targetId"] = new() { StringValue = log.TargetId },
                ["targetTitle"] = new() { StringValue = log.TargetTitle },
                ["details"] = new() { StringValue = log.Details },
                ["timestamp"] = new() { TimestampValue = log.Timestamp.ToString("o") }
            }
        };
    }

    private static string GetString(Dictionary<string, FirestoreValue> fields, string key)
        => fields.TryGetValue(key, out var v) ? v.StringValue ?? "" : "";

    private static DateTime GetTimestamp(Dictionary<string, FirestoreValue> fields, string key)
        => fields.TryGetValue(key, out var v) && DateTime.TryParse(v.TimestampValue, out var dt) ? dt : DateTime.UtcNow;
}
