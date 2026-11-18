namespace HIS.Core.Entities;

public class OrderMaster
{
    public string OrderCode { get; set; } = string.Empty;
    public string OrderType { get; set; } = string.Empty;
    public string OrderName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public bool IsCovered { get; set; }
    public string? Unit { get; set; }
}
