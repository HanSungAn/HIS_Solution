using HIS.Core.Entities;
using HIS.Data.Repositories;

namespace HIS.Services;

/// <summary>
/// 의사 진료(EMR) 및 처방전달(OCS) 비즈니스 로직 (진료 시작 상태 전이, SOAP/처방/바이탈 저장)
/// </summary>
public class EmrService(
    EmrRepository emrRepo,
    MasterRepository masterRepo,
    VisitRepository visitRepo,
    SessionContext session,
    AuditService audit)
{
    public Task<IEnumerable<DiseaseMaster>> SearchDiseasesAsync(string keyword) => masterRepo.SearchDiseasesAsync(keyword);
    public Task<IEnumerable<OrderMaster>> SearchOrdersAsync(string keyword) => masterRepo.SearchOrdersAsync(keyword);
    public Task<MedicalRecord?> GetRecordAsync(int visitId) => emrRepo.GetRecordByVisitAsync(visitId);
    public Task<IEnumerable<OrderItem>> GetOrdersAsync(int visitId) => emrRepo.GetOrdersByVisitAsync(visitId);
    public Task<IEnumerable<VitalSign>> GetVitalsAsync(int patientId) => emrRepo.GetVitalsByPatientAsync(patientId);

    public async Task<bool> StartConsultationAsync(Visit visit)
    {
        if (visit.Status != "Wait") return true;
        var ok = await visitRepo.UpdateStatusWithConcurrencyAsync(visit.VisitId, "InCare", visit.RowVer);
        if (ok)
            await audit.LogAsync("View", "EMR", visit.PatientId, $"진료 시작 (VisitId={visit.VisitId})");
        return ok;
    }

    public async Task CompleteConsultationAsync(
        Visit visit,
        MedicalRecord record,
        IEnumerable<OrderItem> orders,
        VitalSign? vital)
    {
        record.VisitId = visit.VisitId;
        record.DoctorId = session.CurrentUser?.UserId ?? visit.DoctorId;
        await emrRepo.SaveConsultationAsync(record, orders, vital, visit.RowVer);
        await audit.LogAsync("Update", "EMR", visit.PatientId, $"진료/처방 저장 및 수납대기 전환 (VisitId={visit.VisitId})");
    }
}
