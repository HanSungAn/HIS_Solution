using System.Collections.ObjectModel;
using DevExpress.Mvvm;
using HIS.Core.Entities;
using HIS.Services;

namespace HIS.WPF.ViewModels;

/// <summary>
/// 3. 진료비 수납 탭 ViewModel
/// </summary>
public class BillingViewModel : ViewModelBase
{
    private readonly BillingService _billing;

    public ObservableCollection<Visit> PayWaitVisits { get; } = [];

    public Visit? SelectedPayVisit
    {
        get => GetValue<Visit?>();
        set => SetValue(value, OnSelectedPayVisitChanged);
    }

    public Payment? PaymentPreview
    {
        get => GetValue<Payment?>();
        set => SetValue(value);
    }

    public string PayMethod
    {
        get => GetValue<string>();
        set => SetValue(value);
    }

    public AsyncCommand ProcessPaymentCommand { get; }

    public event Action<string>? StatusMessageChanged;
    public event Func<Task>? DataChanged;

    public BillingViewModel(BillingService billing)
    {
        _billing = billing;
        PayMethod = "Card";
        ProcessPaymentCommand = new AsyncCommand(ProcessPaymentAsync);
    }

    public void UpdatePayWaitVisits(IEnumerable<Visit> visits)
    {
        PayWaitVisits.Clear();
        foreach (var v in visits.Where(v => v.Status == "PayWait"))
            PayWaitVisits.Add(v);
    }

    private async void OnSelectedPayVisitChanged()
    {
        if (SelectedPayVisit is null)
        {
            PaymentPreview = null;
            return;
        }
        PaymentPreview = await _billing.CalculatePreviewAsync(SelectedPayVisit);
    }

    private async Task ProcessPaymentAsync()
    {
        if (SelectedPayVisit is null || PaymentPreview is null)
        {
            StatusMessageChanged?.Invoke("수납할 환자를 선택하세요.");
            return;
        }

        await _billing.PayAsync(SelectedPayVisit, PaymentPreview, PayMethod);
        PaymentPreview = null;
        if (DataChanged is not null) await DataChanged.Invoke();
        StatusMessageChanged?.Invoke("수납이 완료되었습니다.");
    }
}
