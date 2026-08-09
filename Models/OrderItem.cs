namespace UCPFoodCorner.Models;

public class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int CafeItemId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}