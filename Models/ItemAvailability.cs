namespace UCPFoodCorner.Models;

public class ItemAvailability
{
    public int Id { get; set; }
    public int CafeItemId { get; set; }
    public DateTime AvailableDate { get; set; }
    public bool IsAvailable { get; set; }
}