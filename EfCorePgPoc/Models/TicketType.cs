namespace EfCorePgPoc.Models;

public class TicketType
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public decimal Quantity { get; set; }
    public decimal AvailableQuantity { get; set; }
    public decimal Price { get; set; }
}