using HairSalonStyleBook.Models;

namespace HairSalonStyleBook.Services;

/// <summary>
/// Before/After 시술 비교 CRUD 서비스 인터페이스
/// </summary>
public interface IBeforeAfterService
{
    Task<List<BeforeAfterItem>> GetAllAsync();

    /// <summary>
    /// 서버사이드 페이징 - createdAt 내림차순, limit개씩
    /// </summary>
    Task<(List<BeforeAfterItem> Items, bool HasMore)> GetPageAsync(int limit, DateTime? beforeDate = null, bool publishedOnly = false);

    Task<BeforeAfterItem> CreateAsync(BeforeAfterItem item);
    Task UpdateAsync(BeforeAfterItem item);
    Task DeleteAsync(string id);
}
