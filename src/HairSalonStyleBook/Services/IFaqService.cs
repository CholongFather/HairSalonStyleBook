using HairSalonStyleBook.Models;

namespace HairSalonStyleBook.Services;

/// <summary>
/// FAQ CRUD 서비스 인터페이스
/// </summary>
public interface IFaqService
{
    Task<List<FaqItem>> GetAllAsync();
    Task<FaqItem> CreateAsync(FaqItem item);
    Task UpdateAsync(FaqItem item);
    Task DeleteAsync(string id);
}
