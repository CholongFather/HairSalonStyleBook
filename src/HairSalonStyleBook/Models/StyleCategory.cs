namespace HairSalonStyleBook.Models;

/// <summary>
/// 스타일 카테고리 (성별 + 머리 길이)
/// </summary>
public enum StyleCategory
{
    전체 = -1,
    남성 = 0,
    여성숏 = 1,
    여성단발 = 2,
    여성미디움 = 3,
    여성롱 = 4
}

/// <summary>
/// 카테고리 표시 이름 헬퍼
/// </summary>
public static class StyleCategoryExtensions
{
    public static string ToDisplayName(this StyleCategory category) => category switch
    {
        StyleCategory.전체 => "전체",
        StyleCategory.남성 => "남성",
        StyleCategory.여성숏 => "숏",
        StyleCategory.여성단발 => "단발",
        StyleCategory.여성미디움 => "미디움",
        StyleCategory.여성롱 => "롱",
        _ => category.ToString()
    };

    public static string ToFullName(this StyleCategory category) => category switch
    {
        StyleCategory.전체 => "전체",
        StyleCategory.남성 => "남성",
        StyleCategory.여성숏 => "여성 숏",
        StyleCategory.여성단발 => "여성 단발",
        StyleCategory.여성미디움 => "여성 미디움",
        StyleCategory.여성롱 => "여성 롱",
        _ => category.ToString()
    };

    public static bool IsFemale(this StyleCategory category) =>
        category is StyleCategory.여성숏 or StyleCategory.여성단발 or StyleCategory.여성미디움 or StyleCategory.여성롱;
}

/// <summary>
/// 시술 종류
/// </summary>
public enum ServiceType
{
    커트 = 0,
    펌 = 1,
    염색 = 2
}
