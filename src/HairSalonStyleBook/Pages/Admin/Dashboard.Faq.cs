using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using HairSalonStyleBook.Models;
using HairSalonStyleBook.Services;

namespace HairSalonStyleBook.Pages.Admin;

/// <summary>
/// Dashboard FAQ 관리 탭
/// </summary>
public partial class Dashboard
{
    // FAQ
    private List<FaqItem> _faqItems = new();
    private bool _faqLoading = false;
    private FaqItem? _faqEditing;
    private bool _faqIsNew;
    private bool _faqSaving;
    private bool _faqUploading;
    private string? _expandedFaqId;
    private string _faqCategoryFilter = "전체";

    private IEnumerable<FaqItem> FilteredFaqItems =>
        _faqCategoryFilter == "전체"
            ? _faqItems.OrderBy(f => f.Order)
            : _faqItems.Where(f => f.Category == _faqCategoryFilter).OrderBy(f => f.Order);

    // FAQ 카테고리별 색상 (왼쪽 바 + 배경 tint)
    private static readonly string[] _faqCategoryColors = new[]
    {
        "#7ba383", "#6b8eb5", "#e8847c", "#c9b1d4", "#d4a574",
        "#4d96ff", "#ff6b9d", "#ffd93d", "#6bcb77", "#a855f7",
    };

    private string GetFaqCategoryColor(string category)
    {
        if (string.IsNullOrEmpty(category)) return _faqCategoryColors[0];
        var cats = _faqItems.Select(f => f.Category).Where(c => !string.IsNullOrEmpty(c)).Distinct().OrderBy(c => c).ToList();
        var idx = cats.IndexOf(category);
        return _faqCategoryColors[idx >= 0 ? idx % _faqCategoryColors.Length : 0];
    }

    private string GetFaqCategoryStyle(string category)
    {
        var color = GetFaqCategoryColor(category);
        return $"border-left: 4px solid {color};";
    }

    private static string GetFaqCategoryIcon(string category) => category switch
    {
        "네이버 플레이스" => "bi-geo-alt-fill",
        "톡톡 파트너스" => "bi-chat-dots-fill",
        "카카오 채널" => "bi-chat-heart-fill",
        "토스 결제" => "bi-credit-card-fill",
        "매장 운영" => "bi-shop",
        _ => "bi-question-circle"
    };

    private void ShowFaqEditor()
    {
        _faqEditing = new FaqItem { Order = _faqItems.Count };
        _faqIsNew = true;
    }

    private void EditFaq(FaqItem faq)
    {
        // 복사본 편집
        _faqEditing = new FaqItem
        {
            Id = faq.Id,
            Title = faq.Title,
            Description = faq.Description,
            Category = faq.Category,
            ImageUrls = new List<string>(faq.ImageUrls),
            Order = faq.Order,
            IsPublished = faq.IsPublished,
            CreatedAt = faq.CreatedAt,
        };
        _faqIsNew = false;
    }

    private void CloseFaqEditor() => _faqEditing = null;

    // NOTE: FAQ 대량 삽입은 Python 스크립트로 진행 (CLAUDE.md 참고)
    // Windows bash/curl은 한글 인코딩 깨짐 → PYTHONUTF8=1 python3 사용 필수

    private void ToggleFaqAccordion(string id) =>
        _expandedFaqId = _expandedFaqId == id ? null : id;

    private async Task SaveFaq()
    {
        if (_faqEditing == null) return;
        _faqSaving = true;
        StateHasChanged();

        try
        {
            if (_faqIsNew)
            {
                await FaqService.CreateAsync(_faqEditing);
                await AuditService.LogAsync("Create", _faqEditing.Id, _faqEditing.Title, $"FAQ '{_faqEditing.Title}' 생성");
            }
            else
            {
                await FaqService.UpdateAsync(_faqEditing);
                await AuditService.LogAsync("Update", _faqEditing.Id, _faqEditing.Title, $"FAQ '{_faqEditing.Title}' 수정");
            }

            _faqItems = await FaqService.GetAllAsync();
            _faqEditing = null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Dashboard] FAQ 저장 실패: {ex.Message}");
            _ = ShowToast("FAQ 저장에 실패했습니다.");
        }

        _faqSaving = false;
        StateHasChanged();
    }

    private async Task DeleteFaq(FaqItem faq)
    {
        try
        {
            if (!string.IsNullOrEmpty(faq.ImageUrl))
            {
                try { await ImageService.DeleteAsync(faq.ImageUrl); } catch { }
            }
            await FaqService.DeleteAsync(faq.Id);
            await AuditService.LogAsync("Delete", faq.Id, faq.Title, $"FAQ '{faq.Title}' 삭제");
            _faqItems = await FaqService.GetAllAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Dashboard] FAQ 삭제 실패: {ex.Message}");
            _ = ShowToast("FAQ 삭제에 실패했습니다.");
        }
        StateHasChanged();
    }

    private async Task OnFaqImageSelected(InputFileChangeEventArgs e)
    {
        if (_faqEditing == null) return;

        _faqUploading = true;
        StateHasChanged();

        foreach (var file in e.GetMultipleFiles(10))
        {
            if (file.Size > ImageUploadHelper.MaxFileSize)
            {
                await JS.InvokeVoidAsync("alert", ImageUploadHelper.GetFileSizeExceededMessage(file.Name));
                continue;
            }

            try
            {
                var buffer = new byte[file.Size];
                await using var stream = file.OpenReadStream(ImageUploadHelper.MaxFileSize);
                await stream.ReadExactlyAsync(buffer);
                var url = await ImageService.UploadAsync($"faq_{file.Name}", buffer, file.ContentType);
                _faqEditing.ImageUrls.Add(url);
                StateHasChanged();
            }
            catch { }
        }

        _faqUploading = false;
        StateHasChanged();
    }

    private void RemoveFaqImage(int index)
    {
        if (_faqEditing != null && index >= 0 && index < _faqEditing.ImageUrls.Count)
            _faqEditing.ImageUrls.RemoveAt(index);
    }
}
