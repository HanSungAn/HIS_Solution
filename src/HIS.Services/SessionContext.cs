using HIS.Core.Entities;

namespace HIS.Services;

/// <summary>
/// 현재 로그인한 사용자 정보, 중복 로그인 검증용 세션 토큰 및 세션 만료 시간을 앱 전체에서 공유합니다. (Singleton)
/// </summary>
public class SessionContext
{
    public User? CurrentUser { get; set; }
    public string? CurrentSessionToken { get; set; }
    public int SessionTimeoutMinutes { get; set; } = 60;
    public DateTime SessionExpiresAt { get; set; }

    public void ExtendSession()
    {
        SessionExpiresAt = DateTime.Now.AddMinutes(SessionTimeoutMinutes);
    }
}
