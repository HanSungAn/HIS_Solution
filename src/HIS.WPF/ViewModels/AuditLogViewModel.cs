using System.Collections.ObjectModel;
using DevExpress.Mvvm;
using HIS.Core.Entities;
using HIS.Services;

namespace HIS.WPF.ViewModels;

public record AuditActionFilterOption(string Label, string? ActionType);

/// <summary>
/// 5. 감사 로그 (보안/이력) 탭 ViewModel
/// </summary>
public class AuditLogViewModel : ViewModelBase
{
    private readonly AuditService _audit;

    public DateTime? FromDate
    {
        get => GetValue<DateTime?>();
        set => SetValue(value);
    }

    public DateTime? ToDate
    {
        get => GetValue<DateTime?>();
        set => SetValue(value);
    }

    public string? Keyword
    {
        get => GetValue<string?>();
        set => SetValue(value);
    }

    public IReadOnlyList<AuditActionFilterOption> ActionTypeOptions { get; } =
    [
        new("전체", null),
        new("로그인 (Login)", "Login"),
        new("로그아웃 (Logout)", "Logout"),
        new("엑셀 다운로드 (Export)", "Export"),
        new("조회 (View)", "View"),
        new("등록 (Create)", "Create"),
        new("수정 (Update)", "Update")
    ];

    public AuditActionFilterOption SelectedActionFilter
    {
        get => GetValue<AuditActionFilterOption>();
        set => SetValue(value);
    }

    public int TotalCount
    {
        get => GetValue<int>();
        set => SetValue(value);
    }

    public ObservableCollection<AuditLog> AuditLogs { get; } = [];

    public AsyncCommand SearchCommand { get; }
    public AsyncCommand ResetFilterCommand { get; }
    public AsyncCommand<string?> QuickFilterCommand { get; }

    public AuditLogViewModel(AuditService audit)
    {
        _audit = audit;

        FromDate = DateTime.Today.AddDays(-7);
        ToDate = DateTime.Today;
        SelectedActionFilter = ActionTypeOptions[0];

        SearchCommand = new AsyncCommand(RefreshAsync);
        ResetFilterCommand = new AsyncCommand(ResetFilterAsync);
        QuickFilterCommand = new AsyncCommand<string?>(QuickFilterAsync);
    }

    public async Task RefreshAsync()
    {
        var items = (await _audit.SearchAsync(
            FromDate,
            ToDate,
            SelectedActionFilter?.ActionType,
            Keyword)).ToList();

        AuditLogs.Clear();
        foreach (var log in items)
            AuditLogs.Add(log);

        TotalCount = items.Count;
    }

    private async Task ResetFilterAsync()
    {
        FromDate = DateTime.Today.AddDays(-7);
        ToDate = DateTime.Today;
        Keyword = string.Empty;
        SelectedActionFilter = ActionTypeOptions[0];
        await RefreshAsync();
    }

    private async Task QuickFilterAsync(string? actionType)
    {
        SelectedActionFilter = ActionTypeOptions.FirstOrDefault(x => x.ActionType == actionType)
            ?? ActionTypeOptions[0];
        await RefreshAsync();
    }
}
