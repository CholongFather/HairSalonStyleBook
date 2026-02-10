namespace HairSalonStyleBook.Models;

/// <summary>
/// 스타일 게시물 모델
/// </summary>
public class StylePost
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public StyleCategory Category { get; set; }
    public Gender Gender { get; set; } = Gender.여성;

    /// <summary>
    /// 이미지 URL 목록 (Firebase Storage URL)
    /// </summary>
    public List<string> ImageUrls { get; set; } = new();
    public List<string> Hashtags { get; set; } = new();
    public List<string> RelatedPostIds { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = "admin";
    public bool IsPublished { get; set; } = true;

    /// <summary>
    /// 첫 번째 이미지 URL (썸네일용)
    /// </summary>
    public string ThumbnailUrl => ImageUrls.FirstOrDefault() ?? string.Empty;
}
