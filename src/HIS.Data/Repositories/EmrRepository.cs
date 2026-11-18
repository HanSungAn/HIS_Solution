using Dapper;
using HIS.Core.Entities;

namespace HIS.Data.Repositories;

public class EmrRepository(IDbConnectionFactory dbFactory)
{
    public async Task<MedicalRecord?> GetRecordByVisitAsync(int visitId)
    {
        using var conn = dbFactory.CreateConnection();
        const string sql = "SELECT * FROM dbo.MedicalRecords WHERE VisitId = @VisitId";
        return await conn.QuerySingleOrDefaultAsync<MedicalRecord>(sql, new { VisitId = visitId });
    }

    public async Task<IEnumerable<OrderItem>> GetOrdersByVisitAsync(int visitId)
    {
        using var conn = dbFactory.CreateConnection();
        const string sql = """
            SELECT o.OrderId, o.VisitId, o.OrderCode, m.OrderName, m.OrderType,
                   m.UnitPrice, m.IsCovered, o.Dosage, o.Frequency, o.Days, o.Status, o.ResultValue, o.IsAbnormal
            FROM dbo.Orders o
            INNER JOIN dbo.OrderMaster m ON o.OrderCode = m.OrderCode
            WHERE o.VisitId = @VisitId
            ORDER BY o.OrderId
            """;
        return await conn.QueryAsync<OrderItem>(sql, new { VisitId = visitId });
    }

    public async Task<IEnumerable<VitalSign>> GetVitalsByPatientAsync(int patientId)
    {
        using var conn = dbFactory.CreateConnection();
        const string sql = """
            SELECT TOP 20 * FROM dbo.VitalSigns
            WHERE PatientId = @PatientId
            ORDER BY MeasuredAt DESC
            """;
        return await conn.QueryAsync<VitalSign>(sql, new { PatientId = patientId });
    }

    /// <summary>
    /// 진료기록(SOAP), 처방 오더, 바이탈 사인 저장 및 접수 상태(PayWait) 전환을
    /// 하나의 DB 트랜잭션으로 묶어 처리합니다. (중간에 실패하면 전체 롤백)
    /// </summary>
    public async Task SaveConsultationAsync(MedicalRecord record, IEnumerable<OrderItem> orders, VitalSign? vital, byte[] visitRowVer)
    {
        using var conn = dbFactory.CreateConnection();
        using var tx = conn.BeginTransaction();

        // MERGE문: 해당 VisitId의 진료기록이 이미 있으면 UPDATE, 없으면 INSERT
        const string upsertRecord = """
            MERGE dbo.MedicalRecords AS t
            USING (SELECT @VisitId AS VisitId) AS s ON t.VisitId = s.VisitId
            WHEN MATCHED THEN
                UPDATE SET Subjective = @Subjective, Objective = @Objective, Assessment = @Assessment,
                           PlanNote = @PlanNote, MainKcdCode = @MainKcdCode, RecordedAt = SYSDATETIME()
            WHEN NOT MATCHED THEN
                INSERT (VisitId, DoctorId, Subjective, Objective, Assessment, PlanNote, MainKcdCode)
                VALUES (@VisitId, @DoctorId, @Subjective, @Objective, @Assessment, @PlanNote, @MainKcdCode);
            """;
        await conn.ExecuteAsync(upsertRecord, record, tx);

        await conn.ExecuteAsync("DELETE FROM dbo.Orders WHERE VisitId = @VisitId", new { record.VisitId }, tx);
        const string insertOrder = """
            INSERT INTO dbo.Orders (VisitId, OrderCode, Dosage, Frequency, Days, Status)
            VALUES (@VisitId, @OrderCode, @Dosage, @Frequency, @Days, @Status)
            """;
        foreach (var order in orders)
        {
            order.VisitId = record.VisitId;
            await conn.ExecuteAsync(insertOrder, order, tx);
        }

        if (vital is not null)
        {
            const string insertVital = """
                INSERT INTO dbo.VitalSigns (VisitId, PatientId, SystolicBP, DiastolicBP, Pulse, Temperature)
                VALUES (@VisitId, @PatientId, @SystolicBP, @DiastolicBP, @Pulse, @Temperature)
                """;
            await conn.ExecuteAsync(insertVital, vital, tx);
        }

        var updated = await conn.ExecuteAsync(
            "UPDATE dbo.Visits SET Status = N'PayWait', UpdatedAt = SYSDATETIME() WHERE VisitId = @VisitId AND RowVer = @RowVer",
            new { record.VisitId, RowVer = visitRowVer }, tx);

        if (updated == 0)
            throw new InvalidOperationException("다른 사용자에 의해 접수 상태가 이미 변경되었습니다.");

        tx.Commit();
    }
}
