using HairSalonStyleBook.Models;

namespace HairSalonStyleBook.Services;

/// <summary>
/// 월별 매출 CRUD 서비스
/// </summary>
public interface IRevenueService
{
    /// <summary>특정 연도의 월별 매출 조회</summary>
    Task<List<MonthlyRevenue>> GetByYearAsync(int year);

    /// <summary>특정 월 매출 조회 (없으면 null)</summary>
    Task<MonthlyRevenue?> GetAsync(int year, int month);

    /// <summary>월 매출 저장 (upsert)</summary>
    Task<MonthlyRevenue> SaveAsync(MonthlyRevenue item);

    /// <summary>월 매출 삭제</summary>
    Task DeleteAsync(string id);

    /// <summary>캐시 무효화</summary>
    void InvalidateCache();
}
