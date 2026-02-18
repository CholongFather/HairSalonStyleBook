using HairSalonStyleBook.Models;

namespace HairSalonStyleBook.Services;

/// <summary>
/// 갤러리 CRUD 서비스 인터페이스
/// </summary>
public interface IGalleryService
{
    Task<List<GalleryItem>> GetAllAsync();
    /// <summary>
    /// 서버사이드 페이징 - createdAt 내림차순, limit개씩
    /// </summary>
    Task<(List<GalleryItem> Items, bool HasMore)> GetPageAsync(int limit, DateTime? beforeDate = null);
    Task<GalleryItem> CreateAsync(GalleryItem item);
    Task UpdateAsync(GalleryItem item);
    Task DeleteAsync(string id);
}
