namespace HairSalonStyleBook.Models;

/// <summary>
/// 월별 매출 데이터 (Firestore: monthlyRevenues/{year}-{month:D2})
/// </summary>
public class MonthlyRevenue
{
    /// <summary>문서 ID: "{year}-{month:D2}" (예: "2026-03")</summary>
    public string Id { get; set; } = "";

    /// <summary>연도</summary>
    public int Year { get; set; }

    /// <summary>월 (1~12)</summary>
    public int Month { get; set; }

    /// <summary>카드 결제 금액 (원)</summary>
    public long CardAmount { get; set; }

    /// <summary>현금 결제 금액 (원)</summary>
    public long CashAmount { get; set; }

    /// <summary>합계 (계산 프로퍼티)</summary>
    public long TotalAmount => CardAmount + CashAmount;

    /// <summary>메모/비고</summary>
    public string Memo { get; set; } = "";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>스냅샷 복사본</summary>
    public MonthlyRevenue Clone() => new()
    {
        Id = Id,
        Year = Year,
        Month = Month,
        CardAmount = CardAmount,
        CashAmount = CashAmount,
        Memo = Memo,
        CreatedAt = CreatedAt,
        UpdatedAt = UpdatedAt
    };

    /// <summary>변경 내역 반환 (감사 로그용)</summary>
    public List<string> GetChanges(MonthlyRevenue original)
    {
        var changes = new List<string>();
        if (CardAmount != original.CardAmount)
            changes.Add($"카드: {original.CardAmount:N0} → {CardAmount:N0}");
        if (CashAmount != original.CashAmount)
            changes.Add($"현금: {original.CashAmount:N0} → {CashAmount:N0}");
        if (Memo != original.Memo)
            changes.Add("메모 변경");
        return changes;
    }
}
