namespace HairSalonStyleBook.Models;

/// <summary>
/// 갤러리 항목 모델 (사진 보관 + 꾸미기)
/// </summary>
public class GalleryItem
{
    public string Id { get; set; } = "";
    /// <summary>원본 이미지 URL (불변)</summary>
    public string ImageUrl { get; set; } = "";
    /// <summary>사진 설명</summary>
    public string Description { get; set; } = "";
    /// <summary>현재 꾸미기 설정</summary>
    public GalleryDecoration Decoration { get; set; } = new();
    /// <summary>꾸미기 변경 히스토리 (최대 20개)</summary>
    public List<DecorationHistory> History { get; set; } = new();
    /// <summary>노출 여부</summary>
    public bool IsPublished { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 갤러리 꾸미기 설정
/// </summary>
public class GalleryDecoration
{
    /// <summary>액자 타입: none, polaroid, vintage, film, elegant, simple</summary>
    public string FrameType { get; set; } = "none";
    /// <summary>오버레이 텍스트</summary>
    public string TextContent { get; set; } = "";
    /// <summary>글씨체: default, handwrite, cute, brush, elegant</summary>
    public string TextFont { get; set; } = "default";
    /// <summary>텍스트 위치: top, center, bottom</summary>
    public string TextPosition { get; set; } = "bottom";
    /// <summary>텍스트 색상 (hex)</summary>
    public string TextColor { get; set; } = "#ffffff";
}

/// <summary>
/// 꾸미기 히스토리 항목
/// </summary>
public class DecorationHistory
{
    public GalleryDecoration Decoration { get; set; } = new();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
