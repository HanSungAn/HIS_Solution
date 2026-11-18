// [DevExpress.Mvvm] ViewModelBase, AsyncCommand 등 MVVM 핵심 유틸리티 제공
using System.Windows;
using System.Windows.Threading;
using DevExpress.Mvvm;
using HIS.Services;
using Microsoft.Data.SqlClient;

namespace HIS.WPF.ViewModels;

/// <summary>
/// 메인 쉘 ViewModel: 각 탭별 ViewModel을 조합하고 탭 간 데이터 동기화 및 상태 메시지를 관리합니다.
/// [DevExpress] ViewModelBase 상속: GetValue/SetValue로 INotifyPropertyChanged 보일러플레이트를 제거합니다.
/// </summary>
public class MainViewModel : ViewModelBase
{
    private readonly SessionContext _session;
    private readonly AuthService _authService;
    private readonly ReceptionService _receptionService;
    private readonly DispatcherTimer _sessionCheckTimer;
    private readonly DispatcherTimer _countdownTimer;

    public string CurrentUserDisplay => _session.CurrentUser is null
        ? string.Empty
        : $"{_session.CurrentUser.UserName} ({_session.CurrentUser.Role})";

    public string RemainingSessionText
    {
        get => GetValue<string>();
        set => SetValue(value);
    }

    public string StatusMessage
    {
        get => GetValue<string>();
        set => SetValue(value);
    }

    // --- 탭별 분리된 하위 ViewModel ---
    public ReceptionViewModel Reception { get; }
    public EmrViewModel Emr { get; }
    public BillingViewModel Billing { get; }
    public PaymentHistoryViewModel PaymentHistory { get; }
    public AuditLogViewModel AuditLog { get; }

    // [DevExpress] AsyncCommand: 비동기 Task 작업을 버튼 Command에 연결 (실행 중 자동 비활성화 지원)
    public AsyncCommand RefreshAllCommand { get; }
    public AsyncCommand LogoutCommand { get; }
    public DelegateCommand ExtendSessionCommand { get; }

    public event Action? LogoutRequested;

    public MainViewModel(
        SessionContext session,
        AuthService authService,
        ReceptionService receptionService,
        ReceptionViewModel reception,
        EmrViewModel emr,
        BillingViewModel billing,
        PaymentHistoryViewModel paymentHistory,
        AuditLogViewModel auditLog)
    {
        _session = session;
        _authService = authService;
        _receptionService = receptionService;

        Reception = reception;
        Emr = emr;
        Billing = billing;
        PaymentHistory = paymentHistory;
        AuditLog = auditLog;

        Reception.StatusMessageChanged += msg => StatusMessage = msg;
        Emr.StatusMessageChanged += msg => StatusMessage = msg;
        Billing.StatusMessageChanged += msg => StatusMessage = msg;

        Reception.DataChanged += RefreshSharedDataAsync;
        Emr.DataChanged += RefreshSharedDataAsync;
        Billing.DataChanged += RefreshSharedDataAsync;
        PaymentHistory.ExportCompleted += async () =>
        {
            await AuditLog.RefreshAsync();
            StatusMessage = "수납 이력 엑셀 저장 및 감사 로그 기록 완료";
        };

        RefreshAllCommand = new AsyncCommand(LoadInitialDataAsync);
        LogoutCommand = new AsyncCommand(ExecuteLogoutAsync);
        ExtendSessionCommand = new DelegateCommand(ExtendSession);

        // 10초마다 DB의 ActiveSessionToken을 확인해 다른 곳에서 중복 로그인했는지 감지
        _sessionCheckTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(10) };
        _sessionCheckTimer.Tick += async (_, _) => await CheckDuplicateSessionAsync();
        _sessionCheckTimer.Start();

        // 1초마다 로그인 유지 남은 시간 갱신 및 만료 시 자동 로그아웃
        UpdateRemainingSessionText();
        _countdownTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _countdownTimer.Tick += async (_, _) => await OnCountdownTickAsync();
        _countdownTimer.Start();
    }

    private void ExtendSession()
    {
        _session.ExtendSession();
        UpdateRemainingSessionText();
        StatusMessage = $"로그인 유지 시간이 {_session.SessionTimeoutMinutes}분으로 연장되었습니다.";
    }

    private void UpdateRemainingSessionText()
    {
        var remaining = _session.SessionExpiresAt - DateTime.Now;
        if (remaining < TimeSpan.Zero) remaining = TimeSpan.Zero;
        RemainingSessionText = $"남은 시간: {(int)remaining.TotalMinutes:D2}:{remaining.Seconds:D2}";
    }

    private async Task OnCountdownTickAsync()
    {
        var remaining = _session.SessionExpiresAt - DateTime.Now;
        if (remaining > TimeSpan.Zero)
        {
            RemainingSessionText = $"남은 시간: {(int)remaining.TotalMinutes:D2}:{remaining.Seconds:D2}";
            return;
        }

        RemainingSessionText = "남은 시간: 00:00";
        StopSessionTimer();

        try
        {
            await _authService.LogoutAsync();
        }
        catch
        {
            _authService.ClearLocalSession();
        }

        MessageBox.Show(
            $"로그인 유지 시간({_session.SessionTimeoutMinutes}분)이 만료되어 자동 로그아웃됩니다.",
            "세션 만료",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
        LogoutRequested?.Invoke();
    }

    private async Task CheckDuplicateSessionAsync()
    {
        try
        {
            if (!await _authService.IsSessionValidAsync())
            {
                StopSessionTimer();
                _authService.ClearLocalSession();
                MessageBox.Show(
                    "다른 위치에서 동일 계정으로 로그인하여 현재 세션이 종료됩니다.",
                    "중복 로그인 감지",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                LogoutRequested?.Invoke();
            }
        }
        catch
        {
            // 일시적 네트워크 지연 등은 다음 주기에 재확인
        }
    }

    public void StopSessionTimer()
    {
        _sessionCheckTimer.Stop();
        _countdownTimer.Stop();
    }

    public async Task CleanupSessionOnCloseAsync()
    {
        _sessionCheckTimer.Stop();
        try
        {
            if (_session.CurrentUser is not null)
                await _authService.LogoutAsync();
        }
        catch
        {
            _authService.ClearLocalSession();
        }
    }

    private async Task ExecuteLogoutAsync()
    {
        if (MessageBox.Show("로그아웃 하시겠습니까?", "로그아웃 확인", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;

        StopSessionTimer();
        try
        {
            await _authService.LogoutAsync();
        }
        catch (SqlException)
        {
            // DB 연결이 끊어진 상태라도 세션은 정리하고 로그인 화면으로 복귀
            _authService.ClearLocalSession();
        }

        LogoutRequested?.Invoke();
    }

    public async Task LoadInitialDataAsync()
    {
        try
        {
            await Reception.LoadInitialAsync();
            await Emr.LoadMastersAsync();
            await RefreshSharedDataAsync();
            StatusMessage = "데이터 동기화 완료";
        }
        catch (SqlException ex)
        {
            StatusMessage = "DB 연결 실패";
            MessageBox.Show(
                $"데이터베이스에 연결할 수 없습니다.\nSQL Server 실행 여부와 연결 설정을 확인하세요.\n\n상세: {ex.Message}",
                "DB 연결 오류",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        catch (Exception ex)
        {
            StatusMessage = "데이터 동기화 중 오류 발생";
            MessageBox.Show(
                $"데이터를 불러오는 중 오류가 발생했습니다.\n\n{ex.Message}",
                "오류",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private async Task RefreshSharedDataAsync()
    {
        var visits = (await _receptionService.GetTodayVisitsAsync()).ToList();
        Reception.UpdateTodayVisits(visits);
        Emr.UpdateTodayVisits(visits);
        Billing.UpdatePayWaitVisits(visits);

        await AuditLog.RefreshAsync();
        await PaymentHistory.SearchAsync();
    }
}
