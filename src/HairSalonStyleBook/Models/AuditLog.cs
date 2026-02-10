namespace HairSalonStyleBook.Models;

/// <summary>
/// 감사 로그 모델
/// </summary>
public class AuditLog
{
    public string Id { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;     // Create, Update, Delete
    public string TargetId { get; set; } = string.Empty;
    public string TargetTitle { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
