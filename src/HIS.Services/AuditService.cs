using HIS.Core.Entities;
using HIS.Data.Repositories;

namespace HIS.Services;

/// <summary>
/// 개인정보 조회·수정·출력·인증(로그인/로그아웃) 감사 이력 서비스
/// </summary>
public class AuditService(AuditLogRepository repo, SessionContext session)
{
    public async Task LogAsync(string actionType, string module, int? patientId = null, string? detail = null)
    {
        if (session.CurrentUser is null) return;
        await repo.WriteAsync(new AuditLog
        {
            UserId = session.CurrentUser.UserId,
            UserName = session.CurrentUser.UserName,
            ActionType = actionType,
            TargetModule = module,
            TargetPatientId = patientId,
            Detail = detail
        });
    }

    public Task<IEnumerable<AuditLog>> GetRecentAsync(int top = 100) => repo.GetRecentAsync(top);

    public Task<IEnumerable<AuditLog>> SearchAsync(
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string? actionType = null,
        string? keyword = null)
        => repo.SearchAsync(fromDate, toDate, actionType, keyword);
}
