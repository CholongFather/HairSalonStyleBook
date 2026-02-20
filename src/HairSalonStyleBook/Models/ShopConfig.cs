namespace HairSalonStyleBook.Models;

/// <summary>
/// 매장 설정 (WiFi, 계좌 정보)
/// </summary>
public class ShopConfig
{
    // WiFi
    public string WifiName5G { get; set; } = "U+NetAFC8_5G";
    public string WifiName24G { get; set; } = "U+NetAFC8";
    public string WifiPassword { get; set; } = "DE01K7#0E5";

    // 계좌
    public string BankName { get; set; } = "우리은행";
    public string AccountNumber { get; set; } = "-";
    public string AccountHolder { get; set; } = "정*경";

    // SNS
    public string InstagramUrl { get; set; } = "";
    public string KakaoChannelUrl { get; set; } = "";
    public string NaverPlaceId { get; set; } = "1883331965";
    public string NaverSearchKeyword { get; set; } = "산척동 미용실";
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

    // SNS 활성/비활성
    public bool SnsInstagramEnabled { get; set; } = true;
    public bool SnsKakaoEnabled { get; set; } = true;
    public bool SnsNaverPlaceEnabled { get; set; } = true;
    public bool SnsNaverReviewEnabled { get; set; } = true;

    public string Wifi5GQrData => $"WIFI:T:WPA;S:{WifiName5G};P:{WifiPassword};;";
    public string Wifi24GQrData => $"WIFI:T:WPA;S:{WifiName24G};P:{WifiPassword};;";
    public const string AccountQrData = "https://cholongfather.github.io/HairSalonStyleBook/pay";
}
