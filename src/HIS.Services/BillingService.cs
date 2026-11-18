using HIS.Core.Entities;
using HIS.Data.Repositories;

namespace HIS.Services;

/// <summary>
/// 진료비 산정(급여/비급여 본인부담금 계산) 및 수납 처리 서비스
/// </summary>
public class BillingService(
    EmrRepository emrRepo,
    BillingRepository billingRepo,
    SessionContext session,
    AuditService audit)
{
    /// <summary>
    /// 처방 오더 목록을 기반으로 총 진료비, 급여/비급여, 환자 본인부담금을 미리 계산합니다.
    /// (건강보험 30%, 의료급여 10%, 일반 100% 적용 후 10원 미만 절사)
    /// </summary>
    public async Task<Payment> CalculatePreviewAsync(Visit visit)
    {
        var orders = (await emrRepo.GetOrdersByVisitAsync(visit.VisitId)).ToList();
        decimal covered = 0;
        decimal nonCovered = 0;

        foreach (var o in orders)
        {
            var lineTotal = o.UnitPrice * o.Dosage * o.Frequency * o.Days;
            if (o.IsCovered) covered += lineTotal;
            else nonCovered += lineTotal;
        }

        // 보험유형별 본인부담률 적용 (NHI 30%, AID 10%, GEN 100%)
        decimal copayRate = visit.InsuranceType switch
        {
            "AID" => 0.10m,
            "GEN" => 1.00m,
            _ => 0.30m
        };

        var patientCovered = Math.Floor(covered * copayRate / 10m) * 10m;
        return new Payment
        {
            VisitId = visit.VisitId,
            TotalAmount = covered + nonCovered,
            CoveredAmount = covered,
            NonCoveredAmount = nonCovered,
            PatientPaidAmount = patientCovered + nonCovered
        };
    }

    public async Task<int> PayAsync(Visit visit, Payment payment, string payMethod)
    {
        payment.PayMethod = payMethod;
        payment.CashierId = session.CurrentUser?.UserId ?? throw new InvalidOperationException("로그인이 필요합니다.");
        var paymentId = await billingRepo.ProcessPaymentAsync(payment, visit.PatientId, visit.RowVer);
        await audit.LogAsync("Create", "Billing", visit.PatientId,
            $"수납 완료: {payment.PatientPaidAmount:N0}원 ({payMethod}, VisitId={visit.VisitId})");
        return paymentId;
    }

    public Task<IEnumerable<PaymentHistory>> GetPaymentHistoriesAsync(
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string? keyword = null,
        string? payMethod = null)
        => billingRepo.GetPaymentHistoriesAsync(fromDate, toDate, keyword, payMethod);

    public Task<IEnumerable<OrderItem>> GetVisitOrderDetailsAsync(int visitId)
        => emrRepo.GetOrdersByVisitAsync(visitId);
}
