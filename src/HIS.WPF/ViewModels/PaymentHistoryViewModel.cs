using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using ClosedXML.Excel;
using DevExpress.Mvvm;
using HIS.Core.Entities;
using HIS.Services;
using HIS.WPF.Views;
using Microsoft.Win32;

namespace HIS.WPF.ViewModels;

public class PaymentHistoryViewModel : ViewModelBase
{
    private readonly BillingService _billing;
    private readonly AuditService _audit;
    private readonly SessionContext _session;

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

    public IReadOnlyList<string> PayMethods { get; } = ["전체", "Card", "Cash", "Transfer"];

    public string SelectedPayMethod
    {
        get => GetValue<string>();
        set => SetValue(value);
    }

    public ObservableCollection<PaymentHistory> PaymentHistories { get; } = [];

    public PaymentHistory? SelectedHistory
    {
        get => GetValue<PaymentHistory?>();
        set => SetValue(value, OnSelectedHistoryChanged);
    }

    public ObservableCollection<OrderItem> SelectedVisitOrders { get; } = [];

    public int TotalCount
    {
        get => GetValue<int>();
        set => SetValue(value);
    }

    public decimal SumTotalAmount
    {
        get => GetValue<decimal>();
        set => SetValue(value);
    }

    public decimal SumCoveredAmount
    {
        get => GetValue<decimal>();
        set => SetValue(value);
    }

    public decimal SumNonCoveredAmount
    {
        get => GetValue<decimal>();
        set => SetValue(value);
    }

    public decimal SumPatientPaidAmount
    {
        get => GetValue<decimal>();
        set => SetValue(value);
    }

    public AsyncCommand SearchCommand { get; }
    public AsyncCommand ResetFilterCommand { get; }
    public AsyncCommand ExportExcelCommand { get; }

    public event Func<Task>? ExportCompleted;

    public PaymentHistoryViewModel(BillingService billing, AuditService audit, SessionContext session)
    {
        _billing = billing;
        _audit = audit;
        _session = session;

        FromDate = DateTime.Today.AddDays(-30);
        ToDate = DateTime.Today;
        SelectedPayMethod = "전체";

        SearchCommand = new AsyncCommand(SearchAsync);
        ResetFilterCommand = new AsyncCommand(ResetFilterAsync);
        ExportExcelCommand = new AsyncCommand(ExportToExcelAsync);
    }

    public async Task SearchAsync()
    {
        var items = (await _billing.GetPaymentHistoriesAsync(FromDate, ToDate, Keyword, SelectedPayMethod)).ToList();

        PaymentHistories.Clear();
        foreach (var item in items)
            PaymentHistories.Add(item);

        TotalCount = items.Count;
        SumTotalAmount = items.Sum(x => x.TotalAmount);
        SumCoveredAmount = items.Sum(x => x.CoveredAmount);
        SumNonCoveredAmount = items.Sum(x => x.NonCoveredAmount);
        SumPatientPaidAmount = items.Sum(x => x.PatientPaidAmount);

        if (SelectedHistory is null || items.All(x => x.HistoryId != SelectedHistory.HistoryId))
            SelectedHistory = PaymentHistories.FirstOrDefault();
    }

    private async Task ResetFilterAsync()
    {
        FromDate = DateTime.Today.AddDays(-30);
        ToDate = DateTime.Today;
        Keyword = string.Empty;
        SelectedPayMethod = "전체";
        await SearchAsync();
    }

    private async void OnSelectedHistoryChanged()
    {
        SelectedVisitOrders.Clear();
        var history = SelectedHistory;
        if (history is null) return;

        var orders = await _billing.GetVisitOrderDetailsAsync(history.VisitId);
        foreach (var order in orders)
            SelectedVisitOrders.Add(order);
    }

    private async Task ExportToExcelAsync()
    {
        if (PaymentHistories.Count == 0)
        {
            MessageBox.Show("저장할 수납 이력이 없습니다. 먼저 조회해 주세요.", "안내", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        // 1. 다운로드 사유 입력 팝업
        var reasonDialog = new ExcelExportReasonDialog
        {
            Owner = Application.Current.MainWindow
        };
        if (reasonDialog.ShowDialog() != true) return;
        var reason = reasonDialog.DownloadReason;

        // 2. 저장 경로 선택
        var dialog = new SaveFileDialog
        {
            Title = "수납 이력 엑셀 저장",
            Filter = "Excel 통합 문서 (*.xlsx)|*.xlsx",
            FileName = $"수납이력_{DateTime.Now:yyyyMMdd_HHmm}.xlsx"
        };

        if (dialog.ShowDialog() != true) return;

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("수납이력");

        string[] headers =
        [
            "수납일시", "영수증번호", "구분", "차트번호", "환자명", "보험",
            "담당의", "총진료비", "급여", "비급여", "본인부담금", "결제수단", "수납자", "비고"
        ];

        for (int c = 0; c < headers.Length; c++)
        {
            var cell = ws.Cell(1, c + 1);
            cell.Value = headers[c];
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1E3A5F");
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }

        int row = 2;
        foreach (var h in PaymentHistories)
        {
            ws.Cell(row, 1).Value = h.OccurredAt;
            ws.Cell(row, 1).Style.DateFormat.Format = "yyyy-MM-dd HH:mm";
            ws.Cell(row, 2).Value = h.ReceiptNo;
            ws.Cell(row, 3).Value = h.ActionType;
            ws.Cell(row, 4).Value = h.ChartNo;
            ws.Cell(row, 5).Value = h.PatientName;
            ws.Cell(row, 6).Value = h.InsuranceType;
            ws.Cell(row, 7).Value = h.DoctorName;
            ws.Cell(row, 8).Value = h.TotalAmount;
            ws.Cell(row, 9).Value = h.CoveredAmount;
            ws.Cell(row, 10).Value = h.NonCoveredAmount;
            ws.Cell(row, 11).Value = h.PatientPaidAmount;
            ws.Cell(row, 12).Value = h.PayMethod;
            ws.Cell(row, 13).Value = h.CashierName;
            ws.Cell(row, 14).Value = h.Memo ?? string.Empty;

            for (int col = 8; col <= 11; col++)
                ws.Cell(row, col).Style.NumberFormat.Format = "#,##0";

            row++;
        }

        // 합계 행 추가
        ws.Cell(row, 1).Value = $"합계 ({TotalCount:N0}건)";
        ws.Cell(row, 8).Value = SumTotalAmount;
        ws.Cell(row, 9).Value = SumCoveredAmount;
        ws.Cell(row, 10).Value = SumNonCoveredAmount;
        ws.Cell(row, 11).Value = SumPatientPaidAmount;

        var summaryRange = ws.Range(row, 1, row, headers.Length);
        summaryRange.Style.Font.Bold = true;
        summaryRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#EFF6FF");
        for (int col = 8; col <= 11; col++)
            ws.Cell(row, col).Style.NumberFormat.Format = "#,##0";

        ws.Columns().AdjustToContents();
        workbook.SaveAs(dialog.FileName);

        // 3. 감사 이력에 다운로드 계정 정보 및 사유 저장
        var user = _session.CurrentUser;
        var accountInfo = user is null
            ? "미확인 계정"
            : $"{user.LoginId} ({user.UserName}/{user.Role})";
        var detail = $"수납 이력 엑셀 다운로드 ({PaymentHistories.Count:N0}건) | 계정: {accountInfo} | 사유: {reason} | 파일: {Path.GetFileName(dialog.FileName)}";

        await _audit.LogAsync("Export", "PaymentHistory", null, detail);
        if (ExportCompleted is not null) await ExportCompleted.Invoke();

        MessageBox.Show($"엑셀 파일이 저장되었습니다.\n\n{dialog.FileName}", "엑셀 저장 완료", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
