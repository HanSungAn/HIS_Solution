using Dapper;
using HIS.Core.Entities;

namespace HIS.Data.Repositories;

/// <summary>
/// 감사 로그(dbo.AuditLogs) 기록 및 조건별 조회를 담당하는 Repository
/// </summary>
public class AuditLogRepository(IDbConnectionFactory dbFactory)
{
    public async Task WriteAsync(AuditLog log)
    {
        using var conn = dbFactory.CreateConnection();
        const string sql = """
            INSERT INTO dbo.AuditLogs (UserId, UserName, ActionType, TargetModule, TargetPatientId, Detail)
            VALUES (@UserId, @UserName, @ActionType, @TargetModule, @TargetPatientId, @Detail)
            """;
        await conn.ExecuteAsync(sql, log);
    }

    public async Task<IEnumerable<AuditLog>> GetRecentAsync(int top = 100)
    {
        using var conn = dbFactory.CreateConnection();
        const string sql = """
            SELECT TOP (@Top) LogId, UserId, UserName, ActionType, TargetModule, TargetPatientId, Detail, CreatedAt
            FROM dbo.AuditLogs
            ORDER BY LogId DESC
            """;
        return await conn.QueryAsync<AuditLog>(sql, new { Top = top });
    }

    public async Task<IEnumerable<AuditLog>> SearchAsync(
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string? actionType = null,
        string? keyword = null)
    {
        using var conn = dbFactory.CreateConnection();
        const string sql = """
            SELECT TOP 500 LogId, UserId, UserName, ActionType, TargetModule, TargetPatientId, Detail, CreatedAt
            FROM dbo.AuditLogs
            WHERE (@FromDate IS NULL OR CreatedAt >= @FromDate)
              AND (@ToDate IS NULL OR CreatedAt < DATEADD(DAY, 1, @ToDate))
              AND (@ActionType IS NULL OR ActionType = @ActionType)
              AND (@Kw IS NULL OR UserName LIKE @Kw OR Detail LIKE @Kw OR TargetModule LIKE @Kw)
            ORDER BY LogId DESC
            """;

        var trimmedAction = string.IsNullOrWhiteSpace(actionType) ? null : actionType.Trim();
        var trimmedKw = string.IsNullOrWhiteSpace(keyword) ? null : $"%{keyword.Trim()}%";

        return await conn.QueryAsync<AuditLog>(sql, new
        {
            FromDate = fromDate?.Date,
            ToDate = toDate?.Date,
            ActionType = trimmedAction,
            Kw = trimmedKw
        });
    }
}
