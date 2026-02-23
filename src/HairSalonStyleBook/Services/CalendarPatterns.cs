namespace HairSalonStyleBook.Services;

/// <summary>
/// 다꾸 캘린더 배경 패턴 CSS + 계절 기본값
/// Calendar.razor / Home.razor 공통 사용
/// </summary>
public static class CalendarPatterns
{
    /// <summary>
    /// 패턴 키 → CSS background-image 스타일 반환
    /// </summary>
    public static string GetPatternCss(string? pattern) => pattern switch
    {
        "dots" => "background-image:radial-gradient(circle,rgba(0,0,0,0.08) 1px,transparent 1px);background-size:16px 16px;",
        "grid" => "background-image:linear-gradient(rgba(0,0,0,0.05) 1px,transparent 1px),linear-gradient(90deg,rgba(0,0,0,0.05) 1px,transparent 1px);background-size:20px 20px;",
        "stripes" => "background-image:repeating-linear-gradient(45deg,transparent,transparent 10px,rgba(0,0,0,0.03) 10px,rgba(0,0,0,0.03) 20px);",
        "hearts" => "background-image:radial-gradient(circle,rgba(232,132,124,0.15) 2px,transparent 2px);background-size:24px 24px;",
        "stars" => "background-image:radial-gradient(circle,rgba(255,217,61,0.2) 1.5px,transparent 1.5px);background-size:20px 20px;",
        "cherry" => "background-image:radial-gradient(circle,rgba(255,182,193,0.4) 3px,transparent 3px),radial-gradient(circle,rgba(255,160,180,0.25) 2px,transparent 2px);background-size:32px 32px,20px 24px;background-position:0 0,10px 12px;",
        "snowflake" => "background-image:radial-gradient(circle,rgba(180,210,240,0.5) 2px,transparent 2px),radial-gradient(circle,rgba(200,220,250,0.3) 1.5px,transparent 1.5px);background-size:28px 28px,18px 20px;background-position:0 0,14px 10px;",
        "forsythia" => "background-image:radial-gradient(circle,rgba(255,215,0,0.35) 3px,transparent 3px),radial-gradient(circle,rgba(255,200,0,0.2) 2px,transparent 2px);background-size:30px 30px,22px 18px;background-position:0 0,15px 9px;",
        "azalea" => "background-image:radial-gradient(circle,rgba(220,100,150,0.3) 3px,transparent 3px),radial-gradient(circle,rgba(240,120,170,0.2) 2px,transparent 2px);background-size:34px 34px,22px 26px;background-position:0 0,12px 14px;",
        "hydrangea" => "background-image:radial-gradient(circle,rgba(130,150,220,0.3) 3px,transparent 3px),radial-gradient(circle,rgba(160,130,210,0.25) 2.5px,transparent 2.5px),radial-gradient(circle,rgba(120,170,220,0.2) 2px,transparent 2px);background-size:30px 30px,24px 22px,18px 20px;background-position:0 0,10px 14px,20px 6px;",
        "sunflower" => "background-image:radial-gradient(circle,rgba(255,180,0,0.35) 3.5px,transparent 3.5px),radial-gradient(circle,rgba(180,120,40,0.2) 2px,transparent 2px);background-size:36px 36px,24px 20px;background-position:0 0,18px 10px;",
        "maple" => "background-image:radial-gradient(circle,rgba(210,100,40,0.25) 3px,transparent 3px),radial-gradient(circle,rgba(230,150,50,0.2) 2px,transparent 2px);background-size:32px 32px,20px 24px;background-position:0 0,16px 12px;",
        "cosmos" => "background-image:radial-gradient(circle,rgba(200,100,160,0.25) 2.5px,transparent 2.5px),radial-gradient(circle,rgba(255,255,255,0.3) 1.5px,transparent 1.5px);background-size:28px 28px,18px 22px;background-position:0 0,14px 11px;",
        _ => ""
    };

    /// <summary>
    /// 월별 계절 기본 배경색 + 패턴 (새 월에만 적용)
    /// </summary>
    public static (string bg, string pattern) GetSeasonalDefault(int month) => month switch
    {
        1  => ("#f0f0f8", "snowflake"),
        2  => ("#f5f0f8", "snowflake"),
        3  => ("#fff5f6", "cherry"),
        4  => ("#fffde6", "forsythia"),
        5  => ("#fff0f5", "azalea"),
        6  => ("#f0f0fa", "hydrangea"),
        7  => ("#fffbe6", "sunflower"),
        8  => ("#e8f4fd", "stripes"),
        9  => ("#faf0f5", "cosmos"),
        10 => ("#faf0e5", "maple"),
        11 => ("#f8f0e8", "maple"),
        12 => ("#eef0fa", "snowflake"),
        _  => ("#fffdf7", "none")
    };

    /// <summary>
    /// 패턴 선택 UI용 목록
    /// </summary>
    public static readonly (string key, string label)[] AllPatterns =
    {
        ("none", "없음"), ("dots", "도트"), ("grid", "격자"), ("stripes", "줄무늬"),
        ("hearts", "하트"), ("stars", "별"), ("cherry", "벚꽃"), ("snowflake", "눈꽃"),
        ("forsythia", "개나리"), ("azalea", "진달래"), ("hydrangea", "수국"),
        ("sunflower", "해바라기"), ("maple", "단풍"), ("cosmos", "코스모스")
    };
}
