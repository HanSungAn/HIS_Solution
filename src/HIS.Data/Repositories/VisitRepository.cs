using Dapper;
using HIS.Core.Entities;

namespace HIS.Data.Repositories;

/// <summary>
/// 외래 접수(dbo.Visits) 데이터를 Dapper로 조회/생성/상태변경하는 Repository
/// </summary>
public class VisitRepository(IDbConnectionFactory dbFactory)
{
    /// <summary>
    /// 오늘 날짜의 접수 목록을 조회합니다. (환자/의사 정보 JOIN 포함)
    /// </summary>
    public async Task<IEnumerable<Visit>> GetTodayVisitsAsync(string? status = null, int? doctorId = null)
    {
        using var conn = dbFactory.CreateConnection();
        const string sql = """
            SELECT v.VisitId, v.PatientId, v.DoctorId, v.DeptCode, v.VisitDate, v.Status, v.ChiefMemo,
                   v.CreatedBy, v.CreatedAt, v.UpdatedAt, v.RowVer,
                   p.ChartNo, p.PatientName, p.InsuranceType, u.UserName AS DoctorName
            FROM dbo.Visits v
            INNER JOIN dbo.Patients p ON v.PatientId = p.PatientId
            INNER JOIN dbo.Users u ON v.DoctorId = u.UserId
            WHERE v.VisitDate = CAST(SYSDATETIME() AS DATE)
              AND (@Status IS NULL OR v.Status = @Status)
              AND (@DoctorId IS NULL OR v.DoctorId = @DoctorId)
            ORDER BY v.VisitId ASC
            """;
        return await conn.QueryAsync<Visit>(sql, new { Status = status, DoctorId = doctorId });
    }

    public async Task<int> CreateVisitAsync(Visit visit)
    {
        using var conn = dbFactory.CreateConnection();
        const string sql = """
            INSERT INTO dbo.Visits (PatientId, DoctorId, DeptCode, Status, ChiefMemo, CreatedBy)
            OUTPUT INSERTED.VisitId
            VALUES (@PatientId, @DoctorId, @DeptCode, @Status, @ChiefMemo, @CreatedBy)
            """;
        return await conn.ExecuteScalarAsync<int>(sql, visit);
    }

    /// <summary>
    /// 낙관적 동시성 제어(RowVer 비교)를 통해 접수 상태를 안전하게 변경합니다.
    /// 다른 사용자가 먼저 수정했다면 WHERE RowVer = @RowVer 조건에 걸려 0행이 수정됩니다.
    /// </summary>
    public async Task<bool> UpdateStatusWithConcurrencyAsync(int visitId, string newStatus, byte[] rowVer)
    {
        using var conn = dbFactory.CreateConnection();
        const string sql = """
            UPDATE dbo.Visits
            SET Status = @NewStatus, UpdatedAt = SYSDATETIME()
            WHERE VisitId = @VisitId AND RowVer = @RowVer
            """;
        var rows = await conn.ExecuteAsync(sql, new { VisitId = visitId, NewStatus = newStatus, RowVer = rowVer });
        return rows > 0;
    }
}
