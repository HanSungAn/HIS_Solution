namespace HIS.Core.Entities;

public class Payment
{
    public int PaymentId { get; set; }
    public int VisitId { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal CoveredAmount { get; set; }
    public decimal NonCoveredAmount { get; set; }
    public decimal PatientPaidAmount { get; set; }
    public string PayMethod { get; set; } = "Card";
    public int CashierId { get; set; }
    public DateTime PaidAt { get; set; }
}
