using System.Collections.ObjectModel;
using DevExpress.Mvvm;
using HIS.Core.Entities;
using HIS.Services;

namespace HIS.WPF.ViewModels;

/// <summary>
/// 2. 진료 / EMR · OCS 탭 ViewModel
/// </summary>
public class EmrViewModel : ViewModelBase
{
    private readonly EmrService _emr;

    public ObservableCollection<Visit> TodayVisits { get; } = [];

    public Visit? SelectedEmrVisit
    {
        get => GetValue<Visit?>();
        set => SetValue(value, OnSelectedEmrVisitChanged);
    }

    public string Subjective { get => GetValue<string>(); set => SetValue(value); }
    public string Objective { get => GetValue<string>(); set => SetValue(value); }
    public string Assessment { get => GetValue<string>(); set => SetValue(value); }
    public string PlanNote { get => GetValue<string>(); set => SetValue(value); }
    public string? SelectedKcdCode { get => GetValue<string?>(); set => SetValue(value); }
    public int? SystolicBP { get => GetValue<int?>(); set => SetValue(value); }
    public int? DiastolicBP { get => GetValue<int?>(); set => SetValue(value); }
    public decimal? Temperature { get => GetValue<decimal?>(); set => SetValue(value); }

    public ObservableCollection<DiseaseMaster> Diseases { get; } = [];
    public ObservableCollection<OrderMaster> OrderMasters { get; } = [];
    public OrderMaster? SelectedOrderMaster { get => GetValue<OrderMaster?>(); set => SetValue(value); }
    public ObservableCollection<OrderItem> CurrentOrders { get; } = [];

    public DelegateCommand AddOrderCommand { get; }
    public DelegateCommand<OrderItem> RemoveOrderCommand { get; }
    public AsyncCommand CompleteConsultationCommand { get; }

    public event Action<string>? StatusMessageChanged;
    public event Func<Task>? DataChanged;

    public EmrViewModel(EmrService emr)
    {
        _emr = emr;

        AddOrderCommand = new DelegateCommand(AddOrder);
        RemoveOrderCommand = new DelegateCommand<OrderItem>(o => { if (o != null) CurrentOrders.Remove(o); });
        CompleteConsultationCommand = new AsyncCommand(CompleteConsultationAsync);
    }

    public async Task LoadMastersAsync()
    {
        Diseases.Clear();
        foreach (var dis in await _emr.SearchDiseasesAsync(string.Empty))
            Diseases.Add(dis);

        OrderMasters.Clear();
        foreach (var om in await _emr.SearchOrdersAsync(string.Empty))
            OrderMasters.Add(om);
    }

    public void UpdateTodayVisits(IEnumerable<Visit> visits)
    {
        var selectedId = SelectedEmrVisit?.VisitId;
        TodayVisits.Clear();
        foreach (var v in visits)
            TodayVisits.Add(v);

        if (selectedId.HasValue)
            SelectedEmrVisit = TodayVisits.FirstOrDefault(v => v.VisitId == selectedId.Value);
    }

    private async void OnSelectedEmrVisitChanged()
    {
        var visit = SelectedEmrVisit;
        if (visit is null) return;

        if (visit.Status == "Wait")
        {
            var visitId = visit.VisitId;
            await _emr.StartConsultationAsync(visit);
            if (DataChanged is not null) await DataChanged.Invoke();
            visit = TodayVisits.FirstOrDefault(v => v.VisitId == visitId);
            if (visit is null) return;
        }

        var rec = await _emr.GetRecordAsync(visit.VisitId);
        Subjective = rec?.Subjective ?? visit.ChiefMemo ?? string.Empty;
        Objective = rec?.Objective ?? string.Empty;
        Assessment = rec?.Assessment ?? string.Empty;
        PlanNote = rec?.PlanNote ?? string.Empty;
        SelectedKcdCode = rec?.MainKcdCode ?? Diseases.FirstOrDefault()?.KcdCode;

        CurrentOrders.Clear();
        foreach (var o in await _emr.GetOrdersAsync(visit.VisitId))
            CurrentOrders.Add(o);
    }

    private void AddOrder()
    {
        if (SelectedOrderMaster is null) return;
        CurrentOrders.Add(new OrderItem
        {
            OrderCode = SelectedOrderMaster.OrderCode,
            OrderName = SelectedOrderMaster.OrderName,
            OrderType = SelectedOrderMaster.OrderType,
            UnitPrice = SelectedOrderMaster.UnitPrice,
            IsCovered = SelectedOrderMaster.IsCovered,
            Dosage = 1,
            Frequency = SelectedOrderMaster.OrderType == "Drug" ? 3 : 1,
            Days = SelectedOrderMaster.OrderType == "Drug" ? 3 : 1,
            Status = "Ordered"
        });
    }

    private async Task CompleteConsultationAsync()
    {
        var visit = SelectedEmrVisit;
        if (visit is null)
        {
            StatusMessageChanged?.Invoke("진료할 환자를 선택하세요.");
            return;
        }

        var record = new MedicalRecord
        {
            Subjective = Subjective,
            Objective = Objective,
            Assessment = Assessment,
            PlanNote = PlanNote,
            MainKcdCode = SelectedKcdCode
        };

        VitalSign? vital = (SystolicBP.HasValue || Temperature.HasValue)
            ? new VitalSign
            {
                VisitId = visit.VisitId,
                PatientId = visit.PatientId,
                SystolicBP = SystolicBP,
                DiastolicBP = DiastolicBP,
                Temperature = Temperature
            }
            : null;

        await _emr.CompleteConsultationAsync(visit, record, CurrentOrders, vital);
        if (DataChanged is not null) await DataChanged.Invoke();
        StatusMessageChanged?.Invoke($"{visit.PatientName}님 진료 완료 → 수납대기 전환");
    }
}
