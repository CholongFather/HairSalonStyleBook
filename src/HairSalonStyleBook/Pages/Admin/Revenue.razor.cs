using HairSalonStyleBook.Models;
using HairSalonStyleBook.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace HairSalonStyleBook.Pages.Admin;

/// <summary>
/// 매출 관리 페이지 코드비하인드
/// </summary>
public partial class Revenue : IAsyncDisposable
{
    [Inject] private IRevenueService RevenueService { get; set; } = default!;
    [Inject] private IAuditService AuditService { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;

    // --- 상태 ---
    private const int MinYear = 2024; // 2024년 11월 오픈
    private bool _loading = true;
    private int _selectedYear = DateTime.Now.Year;

    // 선택 연도 테이블용
    private List<MonthlyRevenue> _revenues = new();
    // 전년 (테이블 전년비용)
    private List<MonthlyRevenue> _prevYearRevenues = new();
    // 전체 데이터 (차트용)
    private List<MonthlyRevenue> _allRevenues = new();

    // --- 연간 요약 ---
    private long _yearTotal, _yearCard, _yearCash;
    private long _prevYearTotal;

    // --- 편집 모달 ---
    private bool _editing;
    private int _editMonth;
    private MonthlyRevenue _editItem = new();
    private MonthlyRevenue? _existingItem;
    private bool _saving;

    // --- Toast ---
    private string _toastMessage = "";
    private bool _toastVisible;

    // --- 차트 렌더링 플래그 ---
    private bool _chartReady;

    protected override async Task OnInitializedAsync()
    {
        await LoadData();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_chartReady)
        {
            _chartReady = false;
            await RenderChart();
        }
    }

    private async Task LoadData()
    {
        _loading = true;
        StateHasChanged();

        try
        {
            // 전체 데이터 한 번에 로드
            _allRevenues = await RevenueService.GetAllAsync();

            // 선택 연도 + 전년 필터
            _revenues = _allRevenues
                .Where(r => r.Year == _selectedYear)
                .OrderBy(r => r.Month).ToList();
            _prevYearRevenues = _allRevenues
                .Where(r => r.Year == _selectedYear - 1)
                .OrderBy(r => r.Month).ToList();

            // 연간 요약
            _yearCard = _revenues.Sum(r => r.CardAmount);
            _yearCash = _revenues.Sum(r => r.CashAmount);
            _yearTotal = _yearCard + _yearCash;
            _prevYearTotal = _prevYearRevenues.Sum(r => r.TotalAmount);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Revenue] 데이터 로드 실패: {ex.Message}");
        }
        finally
        {
            _loading = false;
            _chartReady = true;
            StateHasChanged();
        }
    }

    private async Task RenderChart()
    {
        try
        {
            var labels = Enumerable.Range(1, 12).Select(m => $"{m}월").ToArray();

            // 데이터가 있는 연도만 추출 (오름차순)
            var years = _allRevenues
                .Select(r => r.Year)
                .Distinct()
                .OrderBy(y => y)
                .ToList();

            if (years.Count == 0)
            {
                // 데이터 없으면 빈 차트
                await JS.InvokeVoidAsync("revenueChart.render",
                    "revenueChart", labels, Array.Empty<object>());
                return;
            }

            // 연도별 datasets 구성
            var datasets = years.Select(year =>
            {
                var data = new long[12];
                foreach (var r in _allRevenues.Where(r => r.Year == year))
                    if (r.Month >= 1 && r.Month <= 12)
                        data[r.Month - 1] = r.TotalAmount;

                return new { label = $"{year}년", data };
            }).ToArray();

            await JS.InvokeVoidAsync("revenueChart.render",
                "revenueChart", labels, datasets);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Revenue] 차트 렌더링 실패: {ex.Message}");
        }
    }

    private async Task PrevYear()
    {
        if (_selectedYear <= MinYear) return;
        _selectedYear--;
        await LoadData();
    }

    private async Task NextYear()
    {
        if (_selectedYear >= DateTime.Now.Year) return;
        _selectedYear++;
        await LoadData();
    }

    // --- 편집 모달 ---

    private void OpenEditModal(int month, MonthlyRevenue? existing)
    {
        _editMonth = month;
        _existingItem = existing;
        _editItem = existing?.Clone() ?? new MonthlyRevenue
        {
            Year = _selectedYear,
            Month = month,
            CardAmount = 0,
            CashAmount = 0,
            Memo = ""
        };
        _editing = true;
    }

    private void CancelEdit()
    {
        _editing = false;
        _editItem = new();
    }

    private async Task SaveRevenue()
    {
        if (_saving) return;
        _saving = true;

        try
        {
            _editItem.Year = _selectedYear;
            _editItem.Month = _editMonth;
            _editItem.Id = $"{_selectedYear}-{_editMonth:D2}";

            var isNew = _existingItem == null;
            var saved = await RevenueService.SaveAsync(_editItem);

            // 감사 로그
            var action = isNew ? "Create" : "Update";
            var details = isNew
                ? $"카드 {saved.CardAmount:N0}원, 현금 {saved.CashAmount:N0}원"
                : string.Join(", ", _editItem.GetChanges(_existingItem!));
            await AuditService.LogAsync(action, saved.Id,
                $"{saved.Year}년 {saved.Month}월 매출", details);

            _editing = false;
            await LoadData();
            _ = ShowToast(isNew ? "매출이 등록되었습니다." : "매출이 수정되었습니다.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Revenue] 저장 실패: {ex.Message}");
            _ = ShowToast("저장 실패. 다시 시도해주세요.");
        }
        finally
        {
            _saving = false;
        }
    }

    private async Task DeleteRevenue()
    {
        if (_saving || _existingItem == null) return;
        _saving = true;

        try
        {
            await RevenueService.DeleteAsync(_existingItem.Id);
            await AuditService.LogAsync("Delete", _existingItem.Id,
                $"{_existingItem.Year}년 {_existingItem.Month}월 매출", "매출 삭제");

            _editing = false;
            await LoadData();
            _ = ShowToast("매출이 삭제되었습니다.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Revenue] 삭제 실패: {ex.Message}");
            _ = ShowToast("삭제 실패. 다시 시도해주세요.");
        }
        finally
        {
            _saving = false;
        }
    }

    // --- Toast ---

    private async Task ShowToast(string message)
    {
        _toastMessage = message;
        _toastVisible = true;
        StateHasChanged();
        await Task.Delay(2000);
        _toastVisible = false;
        StateHasChanged();
        await Task.Delay(300);
        _toastMessage = "";
        StateHasChanged();
    }

    // --- 정리 ---

    public async ValueTask DisposeAsync()
    {
        try { await JS.InvokeVoidAsync("revenueChart.dispose"); }
        catch { /* JSDisconnected 무시 */ }
    }

    // --- 헬퍼 ---

    /// <summary>전년 동월 대비 증감률</summary>
    private string GetYoYChange(int month)
    {
        var current = _revenues.FirstOrDefault(r => r.Month == month)?.TotalAmount ?? 0;
        var prev = _prevYearRevenues.FirstOrDefault(r => r.Month == month)?.TotalAmount ?? 0;
        if (prev == 0) return current > 0 ? "신규" : "";
        var pct = (double)(current - prev) / prev * 100;
        return pct >= 0 ? $"+{pct:F1}%" : $"{pct:F1}%";
    }

    private string GetYoYClass(int month)
    {
        var current = _revenues.FirstOrDefault(r => r.Month == month)?.TotalAmount ?? 0;
        var prev = _prevYearRevenues.FirstOrDefault(r => r.Month == month)?.TotalAmount ?? 0;
        if (prev == 0) return current > 0 ? "text-primary" : "";
        return current >= prev ? "rev-up" : "rev-down";
    }
}
