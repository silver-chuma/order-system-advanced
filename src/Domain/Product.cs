namespace Domain;

// Represents a product in inventory
public class Product
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int Stock { get; set; }

    // Concurrency token (manual for SQLite)
    public byte[] RowVersion { get; set; } = Guid.NewGuid().ToByteArray();
}