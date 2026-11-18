namespace HIS.Core.Entities;

public class PaymentHistory
{
    public long HistoryId { get; set; }
    public int PaymentId { get; set; }
    public int VisitId { get; set; }
    public int PatientId { get; set; }
    public string ActionType { get; set; } = "Paid";
    public string ReceiptNo { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public decimal CoveredAmount { get; set; }
    public decimal NonCoveredAmount { get; set; }
    public decimal PatientPaidAmount { get; set; }
    public string PayMethod { get; set; } = "Card";
    public int CashierId { get; set; }
    public string? Memo { get; set; }
    public DateTime OccurredAt { get; set; }

    // Joined display fields
    public string ChartNo { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
    public string InsuranceType { get; set; } = string.Empty;
    public string DeptCode { get; set; } = string.Empty;
    public string DoctorName { get; set; } = string.Empty;
    public string CashierName { get; set; } = string.Empty;
    public DateTime VisitDate { get; set; }
}
