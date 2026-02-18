using System.Text.RegularExpressions;

namespace HairSalonStyleBook.Services;

/// <summary>
/// 간단한 HTML 살균기 - 허용된 태그/속성만 통과, 나머지 제거
/// </summary>
public static partial class HtmlSanitizer
{
    // 허용 태그 (RichTextEditor에서 생성하는 태그만)
    private static readonly HashSet<string> AllowedTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "p", "br", "b", "i", "em", "strong", "u", "s",
        "h1", "h2", "h3", "h4", "h5", "h6",
        "ul", "ol", "li", "blockquote",
        "span", "div", "a", "sub", "sup"
    };

    // 허용 속성
    private static readonly HashSet<string> AllowedAttributes = new(StringComparer.OrdinalIgnoreCase)
    {
        "class", "style", "href", "target", "rel"
    };

    /// <summary>
    /// HTML에서 위험한 태그/속성을 제거하고 안전한 HTML만 반환
    /// </summary>
    public static string Sanitize(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return string.Empty;

        // 1) script, iframe, object, embed, form 태그 완전 제거 (내용 포함)
        html = DangerousTagRegex().Replace(html, string.Empty);

        // 2) on* 이벤트 핸들러 속성 제거 (onclick, onerror 등)
        html = EventHandlerRegex().Replace(html, string.Empty);

        // 3) javascript: 프로토콜 제거
        html = JavascriptProtocolRegex().Replace(html, "href=\"\"");

        // 4) data: 프로토콜 제거 (이미지 인젝션 방지)
        html = DataProtocolRegex().Replace(html, "src=\"\"");

        // 5) 허용되지 않은 태그 제거 (내용은 유지)
        html = TagRegex().Replace(html, match =>
        {
            var tagName = match.Groups[1].Value.TrimStart('/');
            if (AllowedTags.Contains(tagName))
            {
                // 허용 태그면 속성 필터링
                if (match.Value.StartsWith("</"))
                    return match.Value; // 닫는 태그는 그대로

                return FilterAttributes(match.Value, tagName);
            }
            return string.Empty; // 허용되지 않은 태그 제거
        });

        return html;
    }

    private static string FilterAttributes(string tag, string tagName)
    {
        // 속성 파싱
        var attrMatches = AttributeRegex().Matches(tag);
        var safeAttrs = new List<string>();

        foreach (Match attr in attrMatches)
        {
            var attrName = attr.Groups[1].Value;
            if (AllowedAttributes.Contains(attrName))
            {
                // href에 javascript: 차단
                var attrValue = attr.Groups[2].Value;
                if (attrName.Equals("href", StringComparison.OrdinalIgnoreCase) &&
                    attrValue.Contains("javascript:", StringComparison.OrdinalIgnoreCase))
                    continue;

                safeAttrs.Add(attr.Value);
            }
        }

        return safeAttrs.Count > 0
            ? $"<{tagName} {string.Join(" ", safeAttrs)}>"
            : $"<{tagName}>";
    }

    [GeneratedRegex(@"<(script|iframe|object|embed|form|meta|link|base)\b[^>]*>.*?</\1>|<(script|iframe|object|embed|form|meta|link|base)\b[^>]*/?>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex DangerousTagRegex();

    [GeneratedRegex(@"\s+on\w+\s*=\s*(?:""[^""]*""|'[^']*'|[^\s>]+)", RegexOptions.IgnoreCase)]
    private static partial Regex EventHandlerRegex();

    [GeneratedRegex(@"href\s*=\s*""javascript:[^""]*""", RegexOptions.IgnoreCase)]
    private static partial Regex JavascriptProtocolRegex();

    [GeneratedRegex(@"src\s*=\s*""data:[^""]*""", RegexOptions.IgnoreCase)]
    private static partial Regex DataProtocolRegex();

    [GeneratedRegex(@"<(/?\w+)(?:\s[^>]*)?>")]
    private static partial Regex TagRegex();

    [GeneratedRegex(@"(\w+)\s*=\s*""([^""]*)""")]
    private static partial Regex AttributeRegex();
}
