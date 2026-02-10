using HairSalonStyleBook.Models;

namespace HairSalonStyleBook.Services;

/// <summary>
/// 감사 로그 서비스 인터페이스
/// </summary>
public interface IAuditService
{
    Task<List<AuditLog>> GetAllAsync();
    Task LogAsync(string action, string targetId, string targetTitle, string details);
}
