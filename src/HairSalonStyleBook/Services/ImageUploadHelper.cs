namespace HairSalonStyleBook.Services;

/// <summary>
/// 이미지 업로드 공통 상수 및 유틸리티
/// </summary>
public static class ImageUploadHelper
{
    /// <summary>
    /// 최대 파일 크기 (5MB)
    /// </summary>
    public const int MaxFileSize = 5 * 1024 * 1024;

    /// <summary>
    /// 파일 크기 초과 시 사용자 경고 메시지
    /// </summary>
    public const string FileSizeExceededMessage = "파일 크기가 5MB를 초과합니다. 더 작은 파일을 선택해주세요.";

    /// <summary>
    /// 파일 크기 초과 시 이름 포함 메시지
    /// </summary>
    public static string GetFileSizeExceededMessage(string fileName)
        => $"{fileName}: 5MB 초과로 건너뜁니다";
}
