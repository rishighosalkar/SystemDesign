namespace FoodCartLLD.Models;

public class MenuItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Category { get; set; } = string.Empty; // e.g. "Burger", "Drink"
    public bool IsAvailable { get; set; } = true;
}
