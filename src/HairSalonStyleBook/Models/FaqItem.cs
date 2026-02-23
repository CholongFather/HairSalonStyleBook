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
    /// 카테고리 (예: "네이버 플레이스", "톡톡 파트너스", "카카오 채널")
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// 스크린샷/이미지 URL 목록 (Firebase Storage)
    /// </summary>
    public List<string> ImageUrls { get; set; } = new();

    /// <summary>
    /// 하위 호환용 단일 이미지 URL (첫 번째 이미지)
    /// </summary>
    public string ImageUrl
    {
        get => ImageUrls.FirstOrDefault() ?? "";
        set
        {
            if (string.IsNullOrEmpty(value))
                return;
            if (ImageUrls.Count == 0)
                ImageUrls.Add(value);
            else
                ImageUrls[0] = value;
        }
    }

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
