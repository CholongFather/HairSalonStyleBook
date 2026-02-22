namespace HairSalonStyleBook.Models;

/// <summary>
/// 시술 Before/After 비교 항목 (Firestore: beforeAfters/{id})
/// 스타일과 1:1 매핑 아님 — 개별 등록
/// </summary>
public class BeforeAfterItem
{
    public string Id { get; set; } = "";

    /// <summary>시술 전 이미지 URL (1장 이상)</summary>
    public List<string> BeforeImageUrls { get; set; } = new();

    /// <summary>시술 후 이미지 URL (1장 이상)</summary>
    public List<string> AfterImageUrls { get; set; } = new();

    /// <summary>시술명 (예: "레이어드 펌")</summary>
    public string Title { get; set; } = "";

    /// <summary>간단 설명</summary>
    public string Description { get; set; } = "";

    /// <summary>카테고리 (남성/여성숏/여성단발/여성미디움/여성롱)</summary>
    public string Category { get; set; } = "";

    /// <summary>해시태그</summary>
    public List<string> Hashtags { get; set; } = new();

    /// <summary>게시 여부</summary>
    public bool IsPublished { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Before 대표 이미지 (첫 번째)</summary>
    public string BeforeThumbnail => BeforeImageUrls.FirstOrDefault() ?? "";

    /// <summary>After 대표 이미지 (첫 번째)</summary>
    public string AfterThumbnail => AfterImageUrls.FirstOrDefault() ?? "";
}
