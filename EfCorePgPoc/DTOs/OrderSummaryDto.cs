namespace EfCorePgPoc.DTOs;

public class OrderSummaryDto
{
    public Guid OrderId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public decimal TotalPrice { get; set; }
    public string Currency { get; set; } = string.Empty;
    public int ItemCount { get; set; }
}