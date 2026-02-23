namespace HairSalonStyleBook.Models;

/// <summary>
/// 매장 설정 (WiFi, 계좌 정보)
/// </summary>
public class ShopConfig
{
    // WiFi
    public string WifiName5G { get; set; } = "";
    public string WifiName24G { get; set; } = "";
    public string WifiPassword { get; set; } = "";

    // 계좌
    public string BankName { get; set; } = "";
    public string AccountNumber { get; set; } = "";
    public string AccountHolder { get; set; } = "";

    // SNS
    public string InstagramUrl { get; set; } = "";
    public string KakaoChannelUrl { get; set; } = "";
    public string NaverPlaceId { get; set; } = "";
    public string NaverSearchKeyword { get; set; } = "";
    public string NaverPlaceUrl => string.IsNullOrEmpty(NaverPlaceId) ? ""
        : string.IsNullOrEmpty(NaverSearchKeyword)
            ? $"https://m.place.naver.com/hairshop/{NaverPlaceId}/home"
            : $"https://map.naver.com/p/search/{Uri.EscapeDataString(NaverSearchKeyword)}/place/{NaverPlaceId}?placePath=/home";
    public string NaverReviewUrl => string.IsNullOrEmpty(NaverPlaceId) ? "" : $"https://m.place.naver.com/hairshop/{NaverPlaceId}/review/visitor";

    // 간편결제
    public string KakaoPayUrl { get; set; } = "";
    public bool KakaoPayEnabled { get; set; } = false;
    public string NaverPayUrl { get; set; } = "";
    public bool NaverPayEnabled { get; set; } = false;

    // 기능 플래그
    /// <summary>다꾸 캘린더 기능 활성화</summary>
    public bool CalendarEnabled { get; set; } = true;

    // SNS 활성/비활성
    public bool SnsInstagramEnabled { get; set; } = true;
    public bool SnsKakaoEnabled { get; set; } = true;
    public bool SnsNaverPlaceEnabled { get; set; } = true;
    public bool SnsNaverReviewEnabled { get; set; } = true;

    public string Wifi5GQrData => $"WIFI:T:WPA;S:{WifiName5G};P:{WifiPassword};;";
    public string Wifi24GQrData => $"WIFI:T:WPA;S:{WifiName24G};P:{WifiPassword};;";
    public const string AccountQrData = "https://cholongfather.github.io/HairSalonStyleBook/pay";

    /// <summary>현재 값의 스냅샷 복사본 생성</summary>
    public ShopConfig Clone() => new()
    {
        WifiName5G = WifiName5G,
        WifiName24G = WifiName24G,
        WifiPassword = WifiPassword,
        BankName = BankName,
        AccountNumber = AccountNumber,
        AccountHolder = AccountHolder,
        InstagramUrl = InstagramUrl,
        KakaoChannelUrl = KakaoChannelUrl,
        NaverPlaceId = NaverPlaceId,
        NaverSearchKeyword = NaverSearchKeyword,
        KakaoPayUrl = KakaoPayUrl,
        KakaoPayEnabled = KakaoPayEnabled,
        NaverPayUrl = NaverPayUrl,
        NaverPayEnabled = NaverPayEnabled,
        SnsInstagramEnabled = SnsInstagramEnabled,
        SnsKakaoEnabled = SnsKakaoEnabled,
        SnsNaverPlaceEnabled = SnsNaverPlaceEnabled,
        SnsNaverReviewEnabled = SnsNaverReviewEnabled,
        CalendarEnabled = CalendarEnabled
    };

    /// <summary>스냅샷 대비 변경된 필드 목록 반환</summary>
    public List<string> GetChanges(ShopConfig original)
    {
        var changes = new List<string>();
        if (WifiName5G != original.WifiName5G) changes.Add($"WiFi 5G: {original.WifiName5G} → {WifiName5G}");
        if (WifiName24G != original.WifiName24G) changes.Add($"WiFi 2.4G: {original.WifiName24G} → {WifiName24G}");
        if (WifiPassword != original.WifiPassword) changes.Add("WiFi 비밀번호 변경");
        if (BankName != original.BankName) changes.Add($"은행: {original.BankName} → {BankName}");
        if (AccountNumber != original.AccountNumber) changes.Add("계좌번호 변경");
        if (AccountHolder != original.AccountHolder) changes.Add($"예금주: {original.AccountHolder} → {AccountHolder}");
        if (InstagramUrl != original.InstagramUrl) changes.Add("인스타그램 URL 변경");
        if (KakaoChannelUrl != original.KakaoChannelUrl) changes.Add("카카오채널 URL 변경");
        if (NaverPlaceId != original.NaverPlaceId) changes.Add($"네이버 PlaceId: {original.NaverPlaceId} → {NaverPlaceId}");
        if (NaverSearchKeyword != original.NaverSearchKeyword) changes.Add($"네이버 검색어: {original.NaverSearchKeyword} → {NaverSearchKeyword}");
        if (KakaoPayUrl != original.KakaoPayUrl) changes.Add("카카오페이 URL 변경");
        if (KakaoPayEnabled != original.KakaoPayEnabled) changes.Add($"카카오페이: {(KakaoPayEnabled ? "활성" : "비활성")}");
        if (NaverPayUrl != original.NaverPayUrl) changes.Add("네이버페이 URL 변경");
        if (NaverPayEnabled != original.NaverPayEnabled) changes.Add($"네이버페이: {(NaverPayEnabled ? "활성" : "비활성")}");
        if (SnsInstagramEnabled != original.SnsInstagramEnabled) changes.Add($"인스타그램: {(SnsInstagramEnabled ? "활성" : "비활성")}");
        if (SnsKakaoEnabled != original.SnsKakaoEnabled) changes.Add($"카카오채널: {(SnsKakaoEnabled ? "활성" : "비활성")}");
        if (SnsNaverPlaceEnabled != original.SnsNaverPlaceEnabled) changes.Add($"네이버플레이스: {(SnsNaverPlaceEnabled ? "활성" : "비활성")}");
        if (SnsNaverReviewEnabled != original.SnsNaverReviewEnabled) changes.Add($"네이버리뷰: {(SnsNaverReviewEnabled ? "활성" : "비활성")}");
        if (CalendarEnabled != original.CalendarEnabled) changes.Add($"다꾸캘린더: {(CalendarEnabled ? "활성" : "비활성")}");
        return changes;
    }
}
