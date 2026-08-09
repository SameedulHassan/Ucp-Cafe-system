namespace UCPFoodCorner.Models;

public class CartItem
{
    public int ItemId { get; set; }
    public int DealId { get; set; }
    public bool IsDeal { get; set; }
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public string ImagePath { get; set; } = "";
}
