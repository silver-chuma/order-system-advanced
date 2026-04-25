namespace Domain;

// Represents a product in inventory
public class Product
{
    public int Id { get; set; }

    // Current stock level
    public int Stock { get; set; }

    // Concurrency token
    // Used by EF Core to detect conflicting updates
    public byte[] RowVersion { get; set; } = new byte[0];
}