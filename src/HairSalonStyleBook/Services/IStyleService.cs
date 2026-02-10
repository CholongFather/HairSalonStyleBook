using HairSalonStyleBook.Models;

namespace HairSalonStyleBook.Services;

/// <summary>
/// 스타일 서비스 인터페이스
/// </summary>
public interface IStyleService
{
    Task<List<StylePost>> GetAllAsync();
    Task<List<StylePost>> GetByCategoryAsync(StyleCategory category);
    Task<List<StylePost>> SearchAsync(string keyword);
    Task<StylePost?> GetByIdAsync(string id);
    Task<StylePost> CreateAsync(StylePost style);
    Task<StylePost> UpdateAsync(StylePost style);
    Task DeleteAsync(string id);
}
