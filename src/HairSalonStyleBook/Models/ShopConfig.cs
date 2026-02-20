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
    public string KakaoPayQrUrl => string.IsNullOrEmpty(KakaoPayUrl) ? "" : $"https://api.qrserver.com/v1/create-qr-code/?size=200x200&data={Uri.EscapeDataString(KakaoPayUrl)}";

    // SNS 활성/비활성
    public bool SnsInstagramEnabled { get; set; } = true;
    public bool SnsKakaoEnabled { get; set; } = true;
    public bool SnsNaverPlaceEnabled { get; set; } = true;
    public bool SnsNaverReviewEnabled { get; set; } = true;

    // QR 코드 URL 자동 생성
    public string Wifi5GQrUrl => $"https://api.qrserver.com/v1/create-qr-code/?size=200x200&data={Uri.EscapeDataString($"WIFI:T:WPA;S:{WifiName5G};P:{WifiPassword};;")}";
    public string Wifi24GQrUrl => $"https://api.qrserver.com/v1/create-qr-code/?size=200x200&data={Uri.EscapeDataString($"WIFI:T:WPA;S:{WifiName24G};P:{WifiPassword};;")}";
    // 계좌 QR → 결제 페이지 URL로 연결 (텍스트 QR은 폰에서 복사 불가)
    public string AccountQrUrl => $"https://api.qrserver.com/v1/create-qr-code/?size=200x200&data={Uri.EscapeDataString("https://cholongfather.github.io/HairSalonStyleBook/pay")}";
    public string InstagramQrUrl => string.IsNullOrEmpty(InstagramUrl) ? "" : $"https://api.qrserver.com/v1/create-qr-code/?size=200x200&data={Uri.EscapeDataString(InstagramUrl)}";
    public string KakaoChannelQrUrl => string.IsNullOrEmpty(KakaoChannelUrl) ? "" : $"https://api.qrserver.com/v1/create-qr-code/?size=200x200&data={Uri.EscapeDataString(KakaoChannelUrl)}";
    public string NaverPlaceQrUrl => string.IsNullOrEmpty(NaverPlaceUrl) ? "" : $"https://api.qrserver.com/v1/create-qr-code/?size=200x200&data={Uri.EscapeDataString(NaverPlaceUrl)}";
    public string NaverReviewQrUrl => string.IsNullOrEmpty(NaverReviewUrl) ? "" : $"https://api.qrserver.com/v1/create-qr-code/?size=200x200&data={Uri.EscapeDataString(NaverReviewUrl)}";
}
