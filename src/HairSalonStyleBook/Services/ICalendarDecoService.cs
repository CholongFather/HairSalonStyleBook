using HairSalonStyleBook.Models;

namespace HairSalonStyleBook.Services;

/// <summary>
/// 다꾸 캘린더 Firestore 서비스 인터페이스
/// </summary>
public interface ICalendarDecoService
{
    /// <summary>특정 월 데이터 조회 (없으면 빈 객체 반환)</summary>
    Task<CalendarMonth> GetMonthAsync(int year, int month);

    /// <summary>게시된 월 목록 조회 (Viewer용, 최신순)</summary>
    Task<List<CalendarMonth>> GetPublishedMonthsAsync();

    /// <summary>월 데이터 저장 (upsert)</summary>
    Task SaveMonthAsync(CalendarMonth data);

    /// <summary>게시 상태 토글</summary>
    Task SetPublishedAsync(string monthId, bool published);

    /// <summary>월 데이터 삭제</summary>
    Task DeleteMonthAsync(string monthId);
}
