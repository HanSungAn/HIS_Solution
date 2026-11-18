namespace HIS.Core.Entities;

public class AuditLog
{
    public long LogId { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string ActionType { get; set; } = string.Empty;
    public string TargetModule { get; set; } = string.Empty;
    public int? TargetPatientId { get; set; }
    public string? Detail { get; set; }
    public DateTime CreatedAt { get; set; }
}
