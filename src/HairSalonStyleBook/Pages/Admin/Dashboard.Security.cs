using HairSalonStyleBook.Models;

namespace HairSalonStyleBook.Pages.Admin;

/// <summary>
/// Dashboard 보안 탭 (감사 로그, 로그인 시도, 디바이스 차단)
/// </summary>
public partial class Dashboard
{
    private int _securitySubTab;
    private List<LoginAttempt> _loginAttempts = new();
    private List<string> _blockedDevices = new();
    private bool _securityLoading;
    private List<AuditLog> _auditLogs = new();
    private bool _auditLoading;
    private System.Threading.Timer? _securityAutoRefreshTimer;
    private const int SecurityRefreshIntervalSeconds = 30;

    private async Task OpenSecurityTab()
    {
        _adminTab = 3;
        if (!_auditLogs.Any())
            await LoadAuditLogs();
        if (!_loginAttempts.Any())
            await LoadSecurityData();
        StartSecurityAutoRefresh();
    }

    private void StartSecurityAutoRefresh()
    {
        StopSecurityAutoRefresh();
        _securityAutoRefreshTimer = new System.Threading.Timer(
            async _ => await InvokeAsync(async () =>
            {
                if (_securityLoading) return;
                await LoadSecurityData();
            }),
            null,
            TimeSpan.FromSeconds(SecurityRefreshIntervalSeconds),
            TimeSpan.FromSeconds(SecurityRefreshIntervalSeconds));
    }

    private void StopSecurityAutoRefresh()
    {
        _securityAutoRefreshTimer?.Dispose();
        _securityAutoRefreshTimer = null;
    }

    private async Task LoadAuditLogs()
    {
        _auditLoading = true;
        StateHasChanged();
        try
        {
            _auditLogs = await AuditService.GetAllAsync();
            _auditLogs = _auditLogs.OrderByDescending(l => l.Timestamp).ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Dashboard] 감사 로그 로드 실패: {ex.Message}");
            _ = ShowToast("감사 로그 로드에 실패했습니다.");
        }
        _auditLoading = false;
        StateHasChanged();
    }

    private static string GetAuditActionLabel(string action) => action switch
    {
        "Create" => "생성",
        "Update" => "수정",
        "Delete" => "삭제",
        _ => action
    };

    private async Task LoadSecurityData()
    {
        _securityLoading = true;
        StateHasChanged();
        try
        {
            var attemptsTask = LoginSecurityService.GetAttemptsAsync();
            var blockedTask = LoginSecurityService.GetBlockedDevicesAsync();
            await Task.WhenAll(attemptsTask, blockedTask);
            _loginAttempts = attemptsTask.Result;
            _blockedDevices = blockedTask.Result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Dashboard] 보안 데이터 로드 실패: {ex.Message}");
            _ = ShowToast("보안 데이터 로드에 실패했습니다.");
        }
        _securityLoading = false;
        StateHasChanged();
    }

    private async Task BlockDevice(string fingerprint)
    {
        try
        {
            await LoginSecurityService.BlockDeviceAsync(fingerprint);
            _blockedDevices = await LoginSecurityService.GetBlockedDevicesAsync();
            StateHasChanged();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Dashboard] 디바이스 차단 실패: {ex.Message}");
            _ = ShowToast("디바이스 차단에 실패했습니다.");
        }
    }

    private async Task UnblockDevice(string fingerprint)
    {
        try
        {
            await LoginSecurityService.UnblockDeviceAsync(fingerprint);
            _blockedDevices = await LoginSecurityService.GetBlockedDevicesAsync();
            StateHasChanged();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Dashboard] 디바이스 차단 해제 실패: {ex.Message}");
            _ = ShowToast("디바이스 차단 해제에 실패했습니다.");
        }
    }
}
