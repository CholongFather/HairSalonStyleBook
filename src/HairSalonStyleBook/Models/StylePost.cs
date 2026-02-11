namespace HairSalonStyleBook.Models;

/// <summary>
/// 스타일 게시물 모델
/// </summary>
public class StylePost
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public StyleCategory Category { get; set; } = StyleCategory.여성숏;
    public ServiceType Service { get; set; } = ServiceType.커트;

    /// <summary>
    /// 이미지 URL 목록 (Firebase Storage URL)
    /// </summary>
    public List<string> ImageUrls { get; set; } = new();
    public List<string> Hashtags { get; set; } = new();
    public List<string> RelatedPostIds { get; set; } = new();

    /// <summary>
    /// 스타일링 팁 (한줄 요약)
    /// </summary>
    public string StylingTip { get; set; } = string.Empty;

    /// <summary>
    /// 시술 난이도 (1~5점)
    /// </summary>
    public int TreatmentDifficulty { get; set; }

    /// <summary>
    /// 관리 난이도 (1~5점)
    /// </summary>
    public int MaintenanceLevel { get; set; }

    /// <summary>
    /// 유지 기간 (예: "2~3개월")
    /// </summary>
    public string Duration { get; set; } = string.Empty;

    /// <summary>
    /// 추천 얼굴형 (예: "둥근형", "긴형", "각진형")
    /// </summary>
    public List<string> RecommendedFaceShapes { get; set; } = new();

    /// <summary>
    /// 추천 모질 (예: "직모", "곱슬", "가는모")
    /// </summary>
    public List<string> RecommendedHairTypes { get; set; } = new();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = "admin";
    public bool IsPublished { get; set; } = true;

    /// <summary>
    /// 첫 번째 이미지 URL (썸네일용)
    /// </summary>
    public string ThumbnailUrl => ImageUrls.FirstOrDefault() ?? string.Empty;
}
