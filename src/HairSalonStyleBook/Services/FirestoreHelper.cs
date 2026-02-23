namespace HairSalonStyleBook.Services;

/// <summary>
/// Firestore 필드 읽기 공통 헬퍼
/// 모든 Firestore 서비스에서 공유
/// </summary>
public static class FirestoreHelper
{
    public static string GetStr(Dictionary<string, FirestoreValue> fields, string key, string fallback = "")
        => fields.TryGetValue(key, out var v) && !string.IsNullOrEmpty(v.StringValue) ? v.StringValue : fallback;

    public static int GetInt(Dictionary<string, FirestoreValue> fields, string key, int fallback = 0)
        => fields.TryGetValue(key, out var v) && int.TryParse(v.IntegerValue, out var n) ? n : fallback;

    public static bool GetBool(Dictionary<string, FirestoreValue> fields, string key, bool fallback = false)
        => fields.TryGetValue(key, out var v) && v.BooleanValue.HasValue ? v.BooleanValue.Value : fallback;

    public static double GetDouble(Dictionary<string, FirestoreValue> fields, string key, double fallback = 0)
        => fields.TryGetValue(key, out var v) && v.DoubleValue.HasValue ? v.DoubleValue.Value : fallback;

    public static DateTime GetTimestamp(Dictionary<string, FirestoreValue> fields, string key)
        => fields.TryGetValue(key, out var v) && DateTime.TryParse(v.TimestampValue, out var dt) ? dt : DateTime.UtcNow;

    public static DateTime? GetNullableTimestamp(Dictionary<string, FirestoreValue> fields, string key)
        => fields.TryGetValue(key, out var v) && DateTime.TryParse(v.TimestampValue, out var dt) ? dt : null;

    public static List<string> GetStringArray(Dictionary<string, FirestoreValue> fields, string key)
    {
        if (!fields.TryGetValue(key, out var v) || v.ArrayValue?.Values == null)
            return new List<string>();
        return v.ArrayValue.Values.Where(x => x.StringValue != null).Select(x => x.StringValue!).ToList();
    }
}
