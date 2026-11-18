namespace HIS.Core.Entities;

public class VitalSign
{
    public int VitalId { get; set; }
    public int VisitId { get; set; }
    public int PatientId { get; set; }
    public int? SystolicBP { get; set; }
    public int? DiastolicBP { get; set; }
    public int? Pulse { get; set; }
    public decimal? Temperature { get; set; }
    public DateTime MeasuredAt { get; set; }
}
