namespace HIS.Core.Entities;

/// <summary>
/// 처방(오더) 항목 엔티티 (DB: dbo.Orders + dbo.OrderMaster 조인 결과)
/// </summary>
public class OrderItem
{
    public int OrderId { get; set; }
    public int VisitId { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public string OrderName { get; set; } = string.Empty;
    public string OrderType { get; set; } = string.Empty; // Drug, Lab, Treat
    public decimal UnitPrice { get; set; }                // 단가
    public bool IsCovered { get; set; }                   // 급여 여부 (true: 급여, false: 비급여)
    public decimal Dosage { get; set; } = 1;              // 1회 투약량/수량
    public int Frequency { get; set; } = 1;               // 1일 횟수
    public int Days { get; set; } = 1;                    // 처방 일수
    public string Status { get; set; } = "Ordered";
    public string? ResultValue { get; set; }              // 검사 결과값 (Lab 전용)
    public bool IsAbnormal { get; set; }                  // 정상 범위 이탈 여부

    // 항목별 총액 = 단가 × 용량 × 횟수 × 일수
    public decimal LineTotal => UnitPrice * Dosage * Frequency * Days;
}
