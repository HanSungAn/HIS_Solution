namespace HIS.Core.Entities;

public class MedicalRecord
{
    public int RecordId { get; set; }
    public int VisitId { get; set; }
    public int DoctorId { get; set; }
    public string? Subjective { get; set; }
    public string? Objective { get; set; }
    public string? Assessment { get; set; }
    public string? PlanNote { get; set; }
    public string? MainKcdCode { get; set; }
    public DateTime RecordedAt { get; set; }
}
