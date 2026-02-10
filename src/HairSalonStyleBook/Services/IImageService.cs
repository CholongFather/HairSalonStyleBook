namespace HairSalonStyleBook.Services;

/// <summary>
/// 이미지 업로드/삭제 서비스 인터페이스
/// </summary>
public interface IImageService
{
    /// <summary>
    /// 이미지 업로드. 성공 시 다운로드 URL 반환
    /// </summary>
    Task<string> UploadAsync(string fileName, byte[] data, string contentType);

    /// <summary>
    /// 이미지 삭제
    /// </summary>
    Task DeleteAsync(string imageUrl);
}
