namespace HIS.Core.Entities;

public class User
{
    public int UserId { get; set; }
    public string LoginId { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? DeptCode { get; set; }
    public string? LicenseNo { get; set; }
    public bool IsActive { get; set; }
    public string? ActiveSessionToken { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime? LastActiveAt { get; set; }
}
