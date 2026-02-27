using HairSalonStyleBook.Models;

namespace HairSalonStyleBook.Pages.Admin;

/// <summary>
/// Dashboard 매장 설정 탭 (매장 정보, 마감 체크리스트, 쓰레기 배출)
/// </summary>
public partial class Dashboard
{
    // 매장 설정
    private ShopConfig _shopConfig = new();
    private ShopConfig _shopConfigSnapshot = new(); // 변경 감지용 원본 스냅샷
    private bool _shopSaving;
    private bool _shopSaved;

    private async Task SaveShopConfig()
    {
        // 변경사항 확인
        var changes = _shopConfig.GetChanges(_shopConfigSnapshot);
        if (changes.Count == 0)
        {
            // 변경 없으면 저장 스킵
            _shopSaved = true;
            StateHasChanged();
            await Task.Delay(2000);
            _shopSaved = false;
            StateHasChanged();
            return;
        }

        _shopSaving = true;
        _shopSaved = false;
        StateHasChanged();

        try
        {
            await ShopConfigService.SaveAsync(_shopConfig);
            var detail = string.Join(", ", changes);
            await AuditService.LogAsync("Update", "shop", "매장설정", $"매장 설정 변경: {detail}");
            _shopConfigSnapshot = _shopConfig.Clone(); // 스냅샷 갱신
            _shopSaved = true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Dashboard] 매장 설정 저장 실패: {ex.Message}");
            _ = ShowToast("매장 설정 저장에 실패했습니다.");
        }

        _shopSaving = false;
        StateHasChanged();
        await Task.Delay(2000);
        _shopSaved = false;
        StateHasChanged();
    }

    // 쓰레기 배출 스케줄
    private readonly List<TrashScheduleItem> _trashSchedule = new()
    {
        new("월 저녁 배출", "화 아침 수거", "", "bi-trash3", "#6b7280"),
        new("목 저녁 배출", "금 아침 수거", "", "bi-trash3", "#6b7280"),
    };

    private record TrashScheduleItem(string Type, string Days, string Time, string Icon, string Color);

    // 마감 체크리스트
    private List<ChecklistGroup> _checklistGroups = new();

    private int CheckedCount => _checklistGroups.SelectMany(g => g.Items).Count(i => i.Checked);
    private int TotalCount => _checklistGroups.SelectMany(g => g.Items).Count();
    private int ChecklistProgress => TotalCount > 0 ? (int)(CheckedCount * 100.0 / TotalCount) : 0;

    private void InitChecklist()
    {
        _checklistGroups = new List<ChecklistGroup>
        {
            new("청소", "bi-droplet", "#2563eb", new()
            {
                new("바닥"),
            }),
            new("정리", "bi-box-seam", "#059669", new()
            {
                new("수건 세탁"),
                new("재고 확인"),
                new("쓰레기"),
                new("예약"),
            }),
            new("마감", "bi-lock", "#d97706", new()
            {
                new("매출"),
                new("에어컨"),
                new("불"),
                new("문단속"),
                new("신발 갈아신기"),
            }),
        };
    }

    private void ToggleCheck(ChecklistItem item)
    {
        item.Checked = !item.Checked;
    }

    private void ResetChecklist()
    {
        foreach (var item in _checklistGroups.SelectMany(g => g.Items))
            item.Checked = false;
    }

    private class ChecklistGroup
    {
        public string Name { get; set; }
        public string Icon { get; set; }
        public string Color { get; set; }
        public List<ChecklistItem> Items { get; set; }
        public ChecklistGroup(string name, string icon, string color, List<ChecklistItem> items)
        {
            Name = name; Icon = icon; Color = color; Items = items;
        }
    }

    private class ChecklistItem
    {
        public string Text { get; set; }
        public bool Checked { get; set; }
        public ChecklistItem(string text) { Text = text; }
    }
}
