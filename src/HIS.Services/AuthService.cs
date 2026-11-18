using HIS.Core.Entities;
using HIS.Core.Security;
using HIS.Data.Repositories;

namespace HIS.Services;

public record LoginResult(bool Success, bool IsDuplicateSession, User? User, string? Message);

/// <summary>
/// 사용자 로그인/로그아웃, 비밀번호 해시 검증 및 중복 로그인 세션 제어를 담당하는 인증 서비스
/// </summary>
public class AuthService(UserRepository userRepo, SessionContext session, AuditService audit)
{
    public async Task<LoginResult> LoginAsync(string loginId, string password, bool forceLogin = false)
    {
        var user = await userRepo.GetByLoginIdAsync(loginId);
        if (user is null)
            return new LoginResult(false, false, null, "아이디 또는 비밀번호가 올바르지 않습니다.");

        var hash = SecurityHelper.HashPassword(password);
        if (!string.Equals(user.PasswordHash, hash, StringComparison.OrdinalIgnoreCase))
            return new LoginResult(false, false, null, "아이디 또는 비밀번호가 올바르지 않습니다.");

        // 이미 활성 세션 토큰이 존재하고 강제 로그인이 아니면 중복 로그인 안내
        if (!forceLogin && !string.IsNullOrEmpty(user.ActiveSessionToken))
        {
            return new LoginResult(false, true, user,
                "이미 다른 곳에서 로그인 중인 계정입니다.\n기존 세션을 종료하고 새로 로그인하시겠습니까?");
        }

        var newToken = Guid.NewGuid().ToString("N");
        await userRepo.UpdateSessionTokenAsync(user.UserId, newToken);

        session.CurrentUser = user;
        session.CurrentSessionToken = newToken;
        session.ExtendSession();

        var detail = forceLogin
            ? $"중복 로그인 - 기존 세션 강제 종료 ({user.LoginId} / {user.UserName} / {user.Role})"
            : $"로그인 성공 ({user.LoginId} / {user.UserName} / {user.Role})";
        await audit.LogAsync("Login", "Auth", null, detail);

        return new LoginResult(true, false, user, null);
    }

    public async Task LogoutAsync()
    {
        var user = session.CurrentUser;
        if (user is null) return;

        // 내가 가진 세션 토큰과 DB 토큰이 같을 때만 DB 토큰 초기화 (강제 로그아웃된 경우 새 세션 보호)
        var dbToken = await userRepo.GetActiveSessionTokenAsync(user.UserId);
        if (string.Equals(dbToken, session.CurrentSessionToken, StringComparison.Ordinal))
        {
            await userRepo.UpdateSessionTokenAsync(user.UserId, null);
        }

        await audit.LogAsync("Logout", "Auth", null, $"로그아웃 ({user.LoginId} / {user.UserName} / {user.Role})");
        ClearLocalSession();
    }

    public void ClearLocalSession()
    {
        session.CurrentUser = null;
        session.CurrentSessionToken = null;
        session.SessionExpiresAt = default;
    }

    public async Task<bool> IsSessionValidAsync()
    {
        if (session.CurrentUser is null || string.IsNullOrEmpty(session.CurrentSessionToken))
            return false;

        var dbToken = await userRepo.GetActiveSessionTokenAsync(session.CurrentUser.UserId);
        return string.Equals(dbToken, session.CurrentSessionToken, StringComparison.Ordinal);
    }
}
