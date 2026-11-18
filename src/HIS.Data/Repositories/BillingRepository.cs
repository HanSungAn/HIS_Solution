using Dapper;
using HIS.Core.Entities;

namespace HIS.Data.Repositories;

/// <summary>
/// 진료비 수납(dbo.Payments) 및 수납 이력(dbo.PaymentHistories)을 처리하는 Repository
/// </summary>
public class BillingRepository(IDbConnectionFactory dbFactory)
{
    /// <summary>
    /// 수납 내역 저장, 수납 이력(영수증) 기록, 접수 상태 완료(Done) 전환을 하나의 트랜잭션으로 처리합니다.
    /// </summary>
    public async Task<int> ProcessPaymentAsync(Payment payment, int patientId, byte[] visitRowVer)
    {
        using var conn = dbFactory.CreateConnection();
        using var tx = conn.BeginTransaction();

        const string insertPayment = """
            INSERT INTO dbo.Payments (VisitId, TotalAmount, CoveredAmount, NonCoveredAmount, PatientPaidAmount, PayMethod, CashierId)
            OUTPUT INSERTED.PaymentId
            VALUES (@VisitId, @TotalAmount, @CoveredAmount, @NonCoveredAmount, @PatientPaidAmount, @PayMethod, @CashierId)
            """;
        var paymentId = await conn.ExecuteScalarAsync<int>(insertPayment, payment, tx);

        var receiptNo = $"RCP-{DateTime.Now:yyyyMMdd}-{paymentId:D4}";
        const string insertHistory = """
            INSERT INTO dbo.PaymentHistories (
                PaymentId, VisitId, PatientId, ActionType, ReceiptNo,
                TotalAmount, CoveredAmount, NonCoveredAmount, PatientPaidAmount,
                PayMethod, CashierId, Memo
            )
            VALUES (
                @PaymentId, @VisitId, @PatientId, N'Paid', @ReceiptNo,
                @TotalAmount, @CoveredAmount, @NonCoveredAmount, @PatientPaidAmount,
                @PayMethod, @CashierId, @Memo
            )
            """;
        await conn.ExecuteAsync(insertHistory, new
        {
            PaymentId = paymentId,
            payment.VisitId,
            PatientId = patientId,
            ReceiptNo = receiptNo,
            payment.TotalAmount,
            payment.CoveredAmount,
            payment.NonCoveredAmount,
            payment.PatientPaidAmount,
            payment.PayMethod,
            payment.CashierId,
            Memo = "외래 진료비 수납"
        }, tx);

        var updated = await conn.ExecuteAsync(
            "UPDATE dbo.Visits SET Status = N'Done', UpdatedAt = SYSDATETIME() WHERE VisitId = @VisitId AND RowVer = @RowVer",
            new { payment.VisitId, RowVer = visitRowVer }, tx);

        if (updated == 0)
            throw new InvalidOperationException("수납 처리 중 접수 상태가 변경되었습니다.");

        tx.Commit();
        return paymentId;
    }

    public async Task<IEnumerable<PaymentHistory>> GetPaymentHistoriesAsync(
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string? keyword = null,
        string? payMethod = null)
    {
        using var conn = dbFactory.CreateConnection();
        const string sql = """
            SELECT TOP 500
                   h.HistoryId, h.PaymentId, h.VisitId, h.PatientId, h.ActionType, h.ReceiptNo,
                   h.TotalAmount, h.CoveredAmount, h.NonCoveredAmount, h.PatientPaidAmount,
                   h.PayMethod, h.CashierId, h.Memo, h.OccurredAt,
                   p.ChartNo, p.PatientName, p.InsuranceType,
                   v.DeptCode, v.VisitDate,
                   doc.UserName AS DoctorName,
                   cash.UserName AS CashierName
            FROM dbo.PaymentHistories h
            INNER JOIN dbo.Patients p ON h.PatientId = p.PatientId
            INNER JOIN dbo.Visits v ON h.VisitId = v.VisitId
            INNER JOIN dbo.Users doc ON v.DoctorId = doc.UserId
            INNER JOIN dbo.Users cash ON h.CashierId = cash.UserId
            WHERE (@FromDate IS NULL OR h.OccurredAt >= @FromDate)
              AND (@ToDate IS NULL OR h.OccurredAt < DATEADD(DAY, 1, @ToDate))
              AND (@Kw IS NULL OR p.ChartNo LIKE @Kw OR p.PatientName LIKE @Kw OR h.ReceiptNo LIKE @Kw)
              AND (@PayMethod IS NULL OR h.PayMethod = @PayMethod)
            ORDER BY h.OccurredAt DESC, h.HistoryId DESC
            """;

        var trimmedKw = string.IsNullOrWhiteSpace(keyword) ? null : $"%{keyword.Trim()}%";
        var trimmedMethod = string.IsNullOrWhiteSpace(payMethod) || payMethod == "전체" ? null : payMethod;

        return await conn.QueryAsync<PaymentHistory>(sql, new
        {
            FromDate = fromDate?.Date,
            ToDate = toDate?.Date,
            Kw = trimmedKw,
            PayMethod = trimmedMethod
        });
    }
}
