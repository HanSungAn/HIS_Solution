using Dapper;
using HIS.Core.Entities;

namespace HIS.Data.Repositories;

/// <summary>
/// 사용자 조회 및 중복 로그인 방지용 세션 토큰 관리 Repository
/// </summary>
public class UserRepository(IDbConnectionFactory dbFactory)
{
    public async Task<User?> GetByLoginIdAsync(string loginId)
    {
        using var conn = dbFactory.CreateConnection();
        const string sql = """
            SELECT UserId, LoginId, PasswordHash, UserName, Role, DeptCode, LicenseNo, IsActive,
                   ActiveSessionToken, LastLoginAt, LastActiveAt
            FROM dbo.Users
            WHERE LoginId = @LoginId AND IsActive = 1
            """;
        return await conn.QuerySingleOrDefaultAsync<User>(sql, new { LoginId = loginId });
    }

    public async Task<IEnumerable<User>> GetDoctorsAsync()
    {
        using var conn = dbFactory.CreateConnection();
        const string sql = """
            SELECT UserId, LoginId, UserName, Role, DeptCode, LicenseNo, IsActive
            FROM dbo.Users
            WHERE Role = N'Doctor' AND IsActive = 1
            ORDER BY DeptCode, UserName
            """;
        return await conn.QueryAsync<User>(sql);
    }

    public async Task UpdateSessionTokenAsync(int userId, string? sessionToken)
    {
        using var conn = dbFactory.CreateConnection();
        const string sql = """
            UPDATE dbo.Users
            SET ActiveSessionToken = @Token,
                LastLoginAt = CASE WHEN @Token IS NOT NULL THEN SYSDATETIME() ELSE LastLoginAt END,
                LastActiveAt = CASE WHEN @Token IS NOT NULL THEN SYSDATETIME() ELSE NULL END
            WHERE UserId = @UserId
            """;
        await conn.ExecuteAsync(sql, new { UserId = userId, Token = sessionToken });
    }

    public async Task<string?> GetActiveSessionTokenAsync(int userId)
    {
        using var conn = dbFactory.CreateConnection();
        const string sql = "SELECT ActiveSessionToken FROM dbo.Users WHERE UserId = @UserId";
        return await conn.QuerySingleOrDefaultAsync<string?>(sql, new { UserId = userId });
    }
}
