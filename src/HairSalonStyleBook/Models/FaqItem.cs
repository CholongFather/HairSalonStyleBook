namespace HairSalonStyleBook.Models;

/// <summary>
/// FAQ 항목 모델
/// </summary>
public class FaqItem
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 스크린샷/이미지 URL (Firebase Storage)
    /// </summary>
    public string ImageUrl { get; set; } = string.Empty;

    /// <summary>
    /// 정렬 순서 (낮을수록 먼저 노출)
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// 게시 여부
    /// </summary>
    public bool IsPublished { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
