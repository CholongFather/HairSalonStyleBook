using Microsoft.AspNetCore.Components;

namespace HairSalonStyleBook.Components;

/// <summary>
/// 미용실 테마 SVG 스티커 라이브러리
/// </summary>
public static class SvgStickerLibrary
{
    public static readonly (string Key, string Label, string Svg)[] Stickers =
    {
        ("scissors", "가위", "<svg viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='1.5'><circle cx='6' cy='6' r='3'/><circle cx='6' cy='18' r='3'/><line x1='20' y1='4' x2='8.12' y2='15.88'/><line x1='14.47' y1='14.48' x2='20' y2='20'/><line x1='8.12' y1='8.12' x2='12' y2='12'/></svg>"),
        ("dryer", "드라이어", "<svg viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='1.5'><path d='M4 14c0-3 2-6 7-6h5l3-4v14l-3-4h-5c-5 0-7-3-7-6z'/><circle cx='10' cy='14' r='2'/><line x1='15' y1='12' x2='18' y2='12'/></svg>"),
        ("comb", "빗", "<svg viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='1.5'><rect x='8' y='2' width='8' height='20' rx='2'/><line x1='10' y1='6' x2='10' y2='14'/><line x1='12' y1='6' x2='12' y2='14'/><line x1='14' y1='6' x2='14' y2='14'/></svg>"),
        ("mirror", "거울", "<svg viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='1.5'><circle cx='12' cy='10' r='7'/><line x1='12' y1='17' x2='12' y2='22'/><line x1='8' y1='22' x2='16' y2='22'/></svg>"),
        ("brush", "브러시", "<svg viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='1.5'><path d='M4 20c2-2 4-4 6-8l4-7 5 5-7 4c-4 2-6 4-8 6z'/><circle cx='16' cy='8' r='1' fill='currentColor'/></svg>"),
        ("spray", "스프레이", "<svg viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='1.5'><rect x='7' y='8' width='10' height='14' rx='2'/><rect x='9' y='4' width='6' height='4' rx='1'/><line x1='12' y1='2' x2='12' y2='4'/><line x1='9' y1='2' x2='9' y2='3'/><line x1='15' y1='2' x2='15' y2='3'/></svg>"),
        ("chair", "의자", "<svg viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='1.5'><path d='M7 13h10v-3c0-2-2-4-5-4s-5 2-5 4v3z'/><rect x='5' y='13' width='14' height='3' rx='1'/><line x1='7' y1='16' x2='6' y2='22'/><line x1='17' y1='16' x2='18' y2='22'/><line x1='12' y1='16' x2='12' y2='22'/></svg>"),
        ("shampoo", "샴푸", "<svg viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='1.5'><path d='M8 8h8v12c0 1-1 2-2 2h-4c-1 0-2-1-2-2V8z'/><rect x='10' y='4' width='4' height='4' rx='1'/><line x1='12' y1='2' x2='12' y2='4'/></svg>"),
        ("clip", "클립", "<svg viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='1.5'><path d='M4 12c0-4 3-7 7-7h2c3 0 6 2 6 5s-3 5-6 5H9c-2 0-4-1.5-4-3.5S6.5 8 9 8h6'/></svg>"),
        ("razor", "면도기", "<svg viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='1.5'><rect x='9' y='2' width='6' height='8' rx='3'/><rect x='11' y='10' width='2' height='12' rx='1'/></svg>"),
        ("curler", "고데기", "<svg viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='1.5'><path d='M8 4v16'/><path d='M16 4v16'/><path d='M8 8h8'/><path d='M8 12h8'/><path d='M8 16h8'/><circle cx='8' cy='4' r='1.5'/><circle cx='16' cy='4' r='1.5'/></svg>"),
        ("heart", "하트", "<svg viewBox='0 0 24 24' fill='currentColor' stroke='none'><path d='M12 21.35l-1.45-1.32C5.4 15.36 2 12.28 2 8.5 2 5.42 4.42 3 7.5 3c1.74 0 3.41.81 4.5 2.09C13.09 3.81 14.76 3 16.5 3 19.58 3 22 5.42 22 8.5c0 3.78-3.4 6.86-8.55 11.54L12 21.35z'/></svg>"),
        ("star", "별", "<svg viewBox='0 0 24 24' fill='currentColor' stroke='none'><path d='M12 2l3.09 6.26L22 9.27l-5 4.87 1.18 6.88L12 17.77l-6.18 3.25L7 14.14 2 9.27l6.91-1.01L12 2z'/></svg>"),
        ("flower", "꽃", "<svg viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='1.5'><circle cx='12' cy='12' r='3' fill='#ffd93d'/><circle cx='12' cy='6' r='3' fill='#f8bbd0'/><circle cx='17' cy='9.5' r='3' fill='#f8bbd0'/><circle cx='15.5' cy='15.5' r='3' fill='#f8bbd0'/><circle cx='8.5' cy='15.5' r='3' fill='#f8bbd0'/><circle cx='7' cy='9.5' r='3' fill='#f8bbd0'/></svg>"),
    };

    /// <summary>SVG 마크업 반환</summary>
    public static MarkupString GetSvg(string key)
    {
        var sticker = Stickers.FirstOrDefault(s => s.Key == key);
        if (sticker == default)
            return new MarkupString($"<span>{key}</span>");

        return new MarkupString($"<span class='cal-svg-icon' title='{sticker.Label}'>{sticker.Svg}</span>");
    }
}
