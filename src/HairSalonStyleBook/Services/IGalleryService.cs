using HairSalonStyleBook.Models;

namespace HairSalonStyleBook.Services;

/// <summary>
/// 갤러리 CRUD 서비스 인터페이스
/// </summary>
public interface IGalleryService
{
    Task<List<GalleryItem>> GetAllAsync();
    Task<GalleryItem> CreateAsync(GalleryItem item);
    Task UpdateAsync(GalleryItem item);
    Task DeleteAsync(string id);
}
