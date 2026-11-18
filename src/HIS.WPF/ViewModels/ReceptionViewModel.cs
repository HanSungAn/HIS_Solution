using System.Collections.ObjectModel;
using DevExpress.Mvvm;
using HIS.Core.Entities;
using HIS.Services;

namespace HIS.WPF.ViewModels;

/// <summary>
/// 1. 외래 접수 / 원무 탭 ViewModel
/// </summary>
public class ReceptionViewModel : ViewModelBase
{
    private readonly ReceptionService _reception;

    public string PatientKeyword
    {
        get => GetValue<string>();
        set => SetValue(value);
    }

    public ObservableCollection<Patient> Patients { get; } = [];

    public Patient? SelectedPatient
    {
        get => GetValue<Patient?>();
        set => SetValue(value);
    }

    public ObservableCollection<User> Doctors { get; } = [];

    public User? SelectedDoctor
    {
        get => GetValue<User?>();
        set => SetValue(value);
    }

    public string NewPatientName { get => GetValue<string>(); set => SetValue(value); }
    public DateTime NewPatientBirth { get => GetValue<DateTime>(); set => SetValue(value); }
    public string NewPatientGender { get => GetValue<string>(); set => SetValue(value); }
    public string NewPatientSsn { get => GetValue<string>(); set => SetValue(value); }
    public string NewPatientPhone { get => GetValue<string>(); set => SetValue(value); }
    public string ChiefMemo { get => GetValue<string>(); set => SetValue(value); }

    public ObservableCollection<Visit> TodayVisits { get; } = [];

    public AsyncCommand SearchPatientsCommand { get; }
    public AsyncCommand RegisterPatientCommand { get; }
    public AsyncCommand CreateVisitCommand { get; }

    public event Action<string>? StatusMessageChanged;
    public event Func<Task>? DataChanged;

    public ReceptionViewModel(ReceptionService reception)
    {
        _reception = reception;

        NewPatientBirth = new DateTime(1990, 1, 1);
        NewPatientGender = "M";

        SearchPatientsCommand = new AsyncCommand(SearchPatientsAsync);
        RegisterPatientCommand = new AsyncCommand(RegisterPatientAsync);
        CreateVisitCommand = new AsyncCommand(CreateVisitAsync);
    }

    public async Task LoadInitialAsync()
    {
        await SearchPatientsAsync();

        Doctors.Clear();
        foreach (var d in await _reception.GetDoctorsAsync())
            Doctors.Add(d);
        SelectedDoctor ??= Doctors.FirstOrDefault();
    }

    public void UpdateTodayVisits(IEnumerable<Visit> visits)
    {
        TodayVisits.Clear();
        foreach (var v in visits)
            TodayVisits.Add(v);
    }

    private async Task SearchPatientsAsync()
    {
        Patients.Clear();
        foreach (var p in await _reception.SearchPatientsAsync(PatientKeyword ?? string.Empty))
            Patients.Add(p);
    }

    private async Task RegisterPatientAsync()
    {
        if (string.IsNullOrWhiteSpace(NewPatientName))
        {
            StatusMessageChanged?.Invoke("환자 성명을 입력하세요.");
            return;
        }

        var patient = new Patient
        {
            PatientName = NewPatientName.Trim(),
            BirthDate = NewPatientBirth,
            Gender = NewPatientGender,
            Phone = NewPatientPhone,
            InsuranceType = "NHI"
        };
        await _reception.RegisterPatientAsync(patient, NewPatientSsn);
        NewPatientName = string.Empty;
        NewPatientSsn = string.Empty;
        NewPatientPhone = string.Empty;

        await SearchPatientsAsync();
        if (DataChanged is not null) await DataChanged.Invoke();
        StatusMessageChanged?.Invoke($"신환 등록 완료: {patient.PatientName} ({patient.ChartNo})");
    }

    private async Task CreateVisitAsync()
    {
        if (SelectedPatient is null || SelectedDoctor is null)
        {
            StatusMessageChanged?.Invoke("환자와 담당 의사를 선택하세요.");
            return;
        }

        await _reception.CreateVisitAsync(
            SelectedPatient.PatientId,
            SelectedDoctor.UserId,
            SelectedDoctor.DeptCode ?? "IM",
            ChiefMemo);

        ChiefMemo = string.Empty;
        if (DataChanged is not null) await DataChanged.Invoke();
        StatusMessageChanged?.Invoke($"{SelectedPatient.PatientName}님 외래 접수 완료");
    }
}
