using HairSalonStyleBook.Models;

namespace HairSalonStyleBook.Pages.Admin;

/// <summary>
/// Dashboard 공통 상태, 초기화, Toast, Dispose
/// </summary>
public partial class Dashboard
{
    // -- 통계 --
    private int _statStyleTotal, _statStylePublished;
    private int _statGalleryTotal, _statGalleryPublished;
    private int _statBATotal, _statBAPublished;
    private int _statFaqTotal, _statFaqPublished;

    // -- 스타일 서브탭 --
    private int _styleSubTab;

    private void GoToBAEditor() => Nav.NavigateTo("admin/before-after");

    // Toast
    private string _toastMessage = "";
    private bool _toastVisible;

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

    private bool _loading = true;
    private int _adminTab;
    private int _shopAdminTab;
    private HashSet<string> _togglingIds = new();

    private void GoToCalendarEditor() => Nav.NavigateTo("calendar");

    protected override async Task OnInitializedAsync()
    {
        _loading = true;
        InitChecklist();

        try
        {
            var stylesTask = StyleService.GetAllAsync();
            var shopTask = ShopConfigService.GetAsync();
            var faqTask = FaqService.GetAllAsync();
            var galleryTask = GalleryService.GetAllAsync();
            var baTask = BAService.GetAllAsync();
            await Task.WhenAll(stylesTask, shopTask, faqTask, galleryTask, baTask);
            _styles = stylesTask.Result.OrderByDescending(s => s.CreatedAt).ToList();
            _shopConfig = shopTask.Result;
            _shopConfigSnapshot = _shopConfig.Clone();
            _faqItems = faqTask.Result;

            // 통계 계산
            _statStyleTotal = _styles.Count;
            _statStylePublished = _styles.Count(s => s.IsPublished);
            var galleryAll = galleryTask.Result;
            _statGalleryTotal = galleryAll.Count;
            _statGalleryPublished = galleryAll.Count(g => g.IsPublished);
            var baAll = baTask.Result;
            _statBATotal = baAll.Count;
            _statBAPublished = baAll.Count(b => b.IsPublished);
            _statFaqTotal = _faqItems.Count;
            _statFaqPublished = _faqItems.Count(f => f.IsPublished);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Dashboard] 초기화 실패: {ex.Message}");
            _ = ShowToast("데이터 로드 실패. 새로고침해주세요.");
        }

        _loading = false;
    }

    public void Dispose()
    {
        StopSecurityAutoRefresh();
    }
}
