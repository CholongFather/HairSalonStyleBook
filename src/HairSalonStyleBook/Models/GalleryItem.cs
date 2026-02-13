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
    /// <summary>해시태그 (검색용)</summary>
    public List<string> Hashtags { get; set; } = new();
    /// <summary>현재 꾸미기 설정</summary>
    public GalleryDecoration Decoration { get; set; } = new();
    /// <summary>꾸미기 변경 히스토리 (최대 20개)</summary>
    public List<DecorationHistory> History { get; set; } = new();
    /// <summary>방문일 (유저가 설정)</summary>
    public DateTime? VisitDate { get; set; }
    /// <summary>노출 여부 (기본 비노출)</summary>
    public bool IsPublished { get; set; } = false;
    /// <summary>유저 편집 잠금 (true면 유저가 수정 불가)</summary>
    public bool IsLocked { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 갤러리 꾸미기 설정
/// </summary>
public class GalleryDecoration
{
    /// <summary>액자 타입: none, polaroid, vintage, film, elegant, simple, shadow, rounded, magazine, neon</summary>
    public string FrameType { get; set; } = "none";
    /// <summary>오버레이 텍스트</summary>
    public string TextContent { get; set; } = "";
    /// <summary>글씨체</summary>
    public string TextFont { get; set; } = "default";
    /// <summary>텍스트 위치 (레거시): top, center, bottom</summary>
    public string TextPosition { get; set; } = "bottom";
    /// <summary>텍스트 색상 (hex)</summary>
    public string TextColor { get; set; } = "#ffffff";
    /// <summary>텍스트 X 좌표 (%, 0~100)</summary>
    public double TextX { get; set; } = 50;
    /// <summary>텍스트 Y 좌표 (%, 0~100)</summary>
    public double TextY { get; set; } = 85;
    /// <summary>텍스트 크기 배율 (0.5~3.0)</summary>
    public double TextScale { get; set; } = 1.0;
    /// <summary>스티커 (이모지)</summary>
    public string Sticker { get; set; } = "";
    /// <summary>스티커 위치 (레거시)</summary>
    public string StickerPosition { get; set; } = "bottom-right";
    /// <summary>스티커 X 좌표 (%, 0~100)</summary>
    public double StickerX { get; set; } = 80;
    /// <summary>스티커 Y 좌표 (%, 0~100)</summary>
    public double StickerY { get; set; } = 80;
    /// <summary>스티커 크기 배율 (0.5~3.0)</summary>
    public double StickerScale { get; set; } = 1.0;
}

/// <summary>
/// 꾸미기 히스토리 항목
/// </summary>
public class DecorationHistory
{
    public GalleryDecoration Decoration { get; set; } = new();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
