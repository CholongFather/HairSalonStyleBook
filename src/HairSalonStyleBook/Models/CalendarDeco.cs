namespace HairSalonStyleBook.Models;

/// <summary>
/// 다꾸 캘린더 월별 문서 (Firestore: calendarDeco/{year-month})
/// </summary>
public class CalendarMonth
{
    /// <summary>문서 ID (예: "2026-02")</summary>
    public string Id { get; set; } = "";

    public int Year { get; set; }
    public int Month { get; set; }

    /// <summary>캘린더 전체 배경색 (hex)</summary>
    public string BackgroundColor { get; set; } = "#fffdf7";

    /// <summary>배경 패턴: none, dots, grid, stripes, hearts, stars</summary>
    public string BackgroundPattern { get; set; } = "none";

    /// <summary>월 커스텀 제목 (예: "2월의 따뜻한 컬러")</summary>
    public string CustomTitle { get; set; } = "";

    /// <summary>셀별 데코 (key: "1"~"31")</summary>
    public Dictionary<string, CalendarCellDeco> CellDecos { get; set; } = new();

    /// <summary>자유 배치 요소 (캔버스 전체, 최대 50개)</summary>
    public List<CalendarFreeElement> FreeElements { get; set; } = new();

    /// <summary>D-Day 기념일 (key: "day", value: 라벨)</summary>
    public Dictionary<string, string> DDays { get; set; } = new();

    /// <summary>게시 여부 (Viewer에게 노출)</summary>
    public bool IsPublished { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 캘린더 개별 셀 (날짜칸) 꾸미기
/// </summary>
public class CalendarCellDeco
{
    /// <summary>셀 배경색 (hex, 비어있으면 기본)</summary>
    public string BackgroundColor { get; set; } = "";

    /// <summary>짧은 메모 (최대 20자)</summary>
    public string Memo { get; set; } = "";

    /// <summary>셀 내 이모지 스티커 (최대 3개)</summary>
    public List<string> Stickers { get; set; } = new();

    /// <summary>메모 텍스트 색상</summary>
    public string TextColor { get; set; } = "";

    /// <summary>데코가 비어있는지 확인</summary>
    public bool IsEmpty =>
        string.IsNullOrEmpty(BackgroundColor) &&
        string.IsNullOrEmpty(Memo) &&
        Stickers.Count == 0;
}

/// <summary>
/// 캔버스 위 자유 배치 요소
/// </summary>
public class CalendarFreeElement
{
    /// <summary>고유 ID</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..12];

    /// <summary>요소 타입: emoji, svg, image, text, washi</summary>
    public string Type { get; set; } = "emoji";

    /// <summary>콘텐츠 (emoji: 이모지 문자, svg: SVG key, image: URL, text: 텍스트, washi: 패턴key)</summary>
    public string Content { get; set; } = "";

    /// <summary>X 좌표 (%, 0~100)</summary>
    public double X { get; set; } = 50;

    /// <summary>Y 좌표 (%, 0~100)</summary>
    public double Y { get; set; } = 50;

    /// <summary>크기 배율 (0.3~5.0)</summary>
    public double Scale { get; set; } = 1.0;

    /// <summary>회전 각도 (0~360)</summary>
    public double Rotation { get; set; } = 0;

    /// <summary>Z-index (레이어 순서, 높을수록 위)</summary>
    public int ZIndex { get; set; } = 1;

    /// <summary>투명도 (0.0~1.0)</summary>
    public double Opacity { get; set; } = 1.0;

    // -- 텍스트 전용 --
    /// <summary>글씨체: default, handwrite, cute, brush, elegant, round, bold, title</summary>
    public string Font { get; set; } = "default";

    /// <summary>텍스트 색상 (hex)</summary>
    public string Color { get; set; } = "#333333";

    // -- 와시테이프 전용 --
    /// <summary>와시테이프 길이 (% 단위)</summary>
    public double Width { get; set; } = 30;

    /// <summary>와시테이프 패턴: stripe, dot, floral, plaid, pastel, holographic</summary>
    public string Pattern { get; set; } = "stripe";

    /// <summary>잠금 여부 (잠기면 이동/편집 불가)</summary>
    public bool IsLocked { get; set; } = false;

    /// <summary>이미지 프레임: none, polaroid, rounded, circle, stamp, shadow</summary>
    public string Frame { get; set; } = "none";
}
