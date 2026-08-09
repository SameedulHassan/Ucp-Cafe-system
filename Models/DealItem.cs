namespace UCPFoodCorner.Models;

public class DealItem
{
    public int Id { get; set; }
    public int DealId { get; set; }
    public int CafeItemId { get; set; }
    public int Quantity { get; set; } = 1;
}
