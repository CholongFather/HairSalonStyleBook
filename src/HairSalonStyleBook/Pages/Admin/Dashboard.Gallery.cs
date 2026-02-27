using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using HairSalonStyleBook.Models;
using HairSalonStyleBook.Services;

namespace HairSalonStyleBook.Pages.Admin;

/// <summary>
/// Dashboard 갤러리 관리 탭
/// </summary>
public partial class Dashboard
{
    // 갤러리
    private List<GalleryItem> _galleryItems = new();
    private HashSet<string> _selectedGalleryIds = new();
    private bool _galleryUploading;
    private int _galleryUploadedCount;
    private int _galleryTotalUploadCount;
    private string _galleryUploadDesc = "";
    private string _galleryPublishFilter = ""; // "", "published", "hidden"

    private IEnumerable<GalleryItem> FilteredAdminGalleryItems => _galleryPublishFilter switch
    {
        "published" => _galleryItems.Where(g => g.IsPublished),
        "hidden" => _galleryItems.Where(g => !g.IsPublished),
        _ => _galleryItems
    };
    private bool _galleryLoaded;
    private GalleryItem? _adminGalleryEditItem;

    // 스타일 검색 모달 (갤러리→스타일)
    private bool _showStyleSearchModal;
    private string _galleryStyleSearch = "";

    private int _gallerySkippedCount;

    // 앵글 선택
    private string _galleryApplyAngle = "";
    private static readonly Dictionary<string, string> _galleryAngleOptions = new()
    {
        { "front", "정면" }, { "side", "측면" }, { "back", "후면" }, { "quarter", "쿼터뷰" }
    };

    private static string AFPct(double v) => $"{v:F1}%";
    private static string AFScl(double v) => $"{v:F2}";

    private void ToggleGalleryAngle(string key) => _galleryApplyAngle = _galleryApplyAngle == key ? "" : key;

    private void OpenStyleSearchModal()
    {
        _galleryStyleSearch = "";
        _galleryApplyAngle = "";
        _showStyleSearchModal = true;
    }

    private void CloseStyleSearchModal() => _showStyleSearchModal = false;

    private IEnumerable<StylePost> FilteredStylesForGallery =>
        _styles.Where(s =>
            string.IsNullOrEmpty(_galleryStyleSearch) ||
            s.Title.Contains(_galleryStyleSearch, StringComparison.OrdinalIgnoreCase) ||
            s.Hashtags.Any(h => h.Contains(_galleryStyleSearch, StringComparison.OrdinalIgnoreCase)));

    private async Task LoadGalleryIfNeeded()
    {
        if (_galleryLoaded) return;
        _galleryLoaded = true;
        try
        {
            _galleryItems = await GalleryService.GetAllAsync();
            _galleryItems = _galleryItems.OrderByDescending(g => g.CreatedAt).ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Dashboard] 갤러리 로드 실패: {ex.Message}");
            _ = ShowToast("갤러리 로드에 실패했습니다.");
            _galleryLoaded = false; // 재시도 허용
        }
        StateHasChanged();
    }

    private async Task OnGalleryFilesSelected(InputFileChangeEventArgs e)
    {
        var files = e.GetMultipleFiles(20);
        if (!files.Any()) return;

        _galleryUploading = true;
        _galleryUploadedCount = 0;
        _gallerySkippedCount = 0;
        _galleryTotalUploadCount = files.Count;
        StateHasChanged();

        foreach (var file in files)
        {
            if (file.Size > ImageUploadHelper.MaxFileSize)
            {
                _gallerySkippedCount++;
                _galleryUploadedCount++;
                StateHasChanged();
                continue;
            }
            try
            {
                var buffer = new byte[file.Size];
                await using var stream = file.OpenReadStream(ImageUploadHelper.MaxFileSize);
                await stream.ReadExactlyAsync(buffer);
                // 이미지 리사이징 (최대 1200px, WebP 변환)
                var base64 = Convert.ToBase64String(buffer);
                var resized = await JS.InvokeAsync<string>("resizeImage", base64, 1200, 1600, 0.82);
                var resizedData = Convert.FromBase64String(resized);
                var webpName = Path.GetFileNameWithoutExtension(file.Name) + ".webp";
                var url = await ImageService.UploadAsync(webpName, resizedData, "image/webp", "gallery");

                var item = new GalleryItem
                {
                    ImageUrl = url,
                    Description = _galleryUploadDesc,
                };
                await GalleryService.CreateAsync(item);
                _galleryItems.Insert(0, item);
            }
            catch { }
            _galleryUploadedCount++;
            StateHasChanged();
        }

        var uploadedCount = _galleryTotalUploadCount - _gallerySkippedCount;
        if (uploadedCount > 0)
            await AuditService.LogAsync("Create", "", "갤러리", $"갤러리 이미지 {uploadedCount}건 업로드");

        _galleryUploading = false;
        _galleryUploadDesc = "";
        StateHasChanged();
    }

    private void ToggleGallerySelect(string id)
    {
        if (!_selectedGalleryIds.Remove(id))
            _selectedGalleryIds.Add(id);
    }

    private void ToggleGallerySelectAll()
    {
        if (_selectedGalleryIds.Count == _galleryItems.Count)
            _selectedGalleryIds.Clear();
        else
            _selectedGalleryIds = _galleryItems.Select(g => g.Id).ToHashSet();
    }

    private async Task ApplyPhotosToStyle(StylePost style)
    {
        try
        {
            var selectedItems = _galleryItems.Where(g => _selectedGalleryIds.Contains(g.Id)).ToList();
            var angleSuffix = string.IsNullOrEmpty(_galleryApplyAngle) ? "" : $"__{_galleryApplyAngle}";
            foreach (var item in selectedItems)
            {
                // 원본 URL에 앵글 태그 추가 (프래그먼트)
                var url = item.ImageUrl;
                if (!string.IsNullOrEmpty(angleSuffix))
                    url = url.Contains('?') ? $"{url}&angle={_galleryApplyAngle}" : $"{url}?angle={_galleryApplyAngle}";
                if (!style.ImageUrls.Any(u => u.Split('?')[0] == item.ImageUrl))
                    style.ImageUrls.Add(url);
            }
            await StyleService.UpdateAsync(style);
            if (selectedItems.Any())
                await AuditService.LogAsync("Update", style.Id, style.Title, $"갤러리 사진 {selectedItems.Count}건 → 스타일 '{style.Title}'에 연결");
            _showStyleSearchModal = false;
            _selectedGalleryIds.Clear();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Dashboard] 스타일에 사진 연결 실패: {ex.Message}");
            _ = ShowToast("사진 연결에 실패했습니다.");
        }
        StateHasChanged();
    }

    private async Task DeleteSelectedGallery()
    {
        try
        {
            var toDelete = _galleryItems.Where(g => _selectedGalleryIds.Contains(g.Id)).ToList();
            foreach (var item in toDelete)
            {
                try { await ImageService.DeleteAsync(item.ImageUrl); } catch { }
                await GalleryService.DeleteAsync(item.Id);
                _galleryItems.Remove(item);
            }
            if (toDelete.Any())
                await AuditService.LogAsync("BulkDelete", "", "갤러리", $"갤러리 이미지 {toDelete.Count}건 삭제");
            _selectedGalleryIds.Clear();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Dashboard] 갤러리 삭제 실패: {ex.Message}");
            _ = ShowToast("갤러리 삭제에 실패했습니다.");
        }
        StateHasChanged();
    }

    private async Task ToggleGalleryPublish(GalleryItem item)
    {
        if (!_togglingIds.Add($"gp_{item.Id}")) return; // 연타 방지
        try
        {
            item.IsPublished = !item.IsPublished;
            await GalleryService.UpdateAsync(item);
            await AuditService.LogAsync("Publish", item.Id, "갤러리", $"갤러리 게시 상태 → {(item.IsPublished ? "게시" : "비게시")}");
            StateHasChanged();
        }
        catch (Exception ex)
        {
            item.IsPublished = !item.IsPublished; // 롤백
            Console.WriteLine($"[Dashboard] 갤러리 게시 토글 실패: {ex.Message}");
            _ = ShowToast("게시 상태 변경에 실패했습니다.");
        }
        finally { _togglingIds.Remove($"gp_{item.Id}"); }
    }

    private async Task ToggleGalleryLock(GalleryItem item)
    {
        if (!_togglingIds.Add($"gl_{item.Id}")) return; // 연타 방지
        try
        {
            item.IsLocked = !item.IsLocked;
            await GalleryService.UpdateAsync(item);
            await AuditService.LogAsync("Update", item.Id, "갤러리", $"갤러리 잠금 → {(item.IsLocked ? "ON" : "OFF")}");
            StateHasChanged();
        }
        catch (Exception ex)
        {
            item.IsLocked = !item.IsLocked; // 롤백
            Console.WriteLine($"[Dashboard] 갤러리 잠금 토글 실패: {ex.Message}");
            _ = ShowToast("잠금 상태 변경에 실패했습니다.");
        }
        finally { _togglingIds.Remove($"gl_{item.Id}"); }
    }

    private void OpenAdminGalleryEdit(GalleryItem item) => _adminGalleryEditItem = item;

    private async Task SaveAdminGalleryDecoration(GalleryItem item)
    {
        try
        {
            await GalleryService.UpdateAsync(item);
            await AuditService.LogAsync("Update", item.Id, "갤러리", "갤러리 꾸미기 저장");
            _adminGalleryEditItem = null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Dashboard] 갤러리 꾸미기 저장 실패: {ex.Message}");
            _ = ShowToast("갤러리 꾸미기 저장에 실패했습니다.");
        }
        StateHasChanged();
    }
}
