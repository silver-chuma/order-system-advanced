namespace Domain;

// Represents a placed order
public class Order
{
    public int Id { get; set; }
    // Ensures duplicate requests don’t create multiple orders
    public string IdempotencyKey { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}