using HIS.Core.Entities;
using HIS.Core.Security;
using HIS.Data.Repositories;

namespace HIS.Services;

/// <summary>
/// 원무/접수 비즈니스 로직 (환자 검색, 신환 등록 시 차트번호 채번·주민번호 마스킹, 외래 접수)
/// </summary>
public class ReceptionService(
    PatientRepository patientRepo,
    VisitRepository visitRepo,
    UserRepository userRepo,
    SessionContext session,
    AuditService audit)
{
    public Task<IEnumerable<Patient>> SearchPatientsAsync(string keyword) => patientRepo.SearchAsync(keyword);
    public Task<IEnumerable<User>> GetDoctorsAsync() => userRepo.GetDoctorsAsync();
    public Task<IEnumerable<Visit>> GetTodayVisitsAsync(string? status = null, int? doctorId = null)
        => visitRepo.GetTodayVisitsAsync(status, doctorId);

    public async Task<int> RegisterPatientAsync(Patient patient, string? rawSsn)
    {
        patient.ChartNo = await patientRepo.GenerateNextChartNoAsync();
        patient.SsnMasked = SecurityHelper.MaskSsn(rawSsn);
        var id = await patientRepo.InsertAsync(patient);
        await audit.LogAsync("Create", "Reception", id, $"신환 등록: {patient.PatientName} ({patient.ChartNo})");
        return id;
    }

    public async Task<int> CreateVisitAsync(int patientId, int doctorId, string deptCode, string? chiefMemo)
    {
        var userId = session.CurrentUser?.UserId ?? throw new InvalidOperationException("로그인이 필요합니다.");
        var visitId = await visitRepo.CreateVisitAsync(new Visit
        {
            PatientId = patientId,
            DoctorId = doctorId,
            DeptCode = deptCode,
            Status = "Wait",
            ChiefMemo = chiefMemo,
            CreatedBy = userId
        });
        await audit.LogAsync("Create", "Reception", patientId, $"외래 접수 (VisitId={visitId})");
        return visitId;
    }
}
