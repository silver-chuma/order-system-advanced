namespace API.DTOs;

// DTO for incoming order request from client
public class CreateOrderRequest
{
    // List of products and quantities in the order
    public List<OrderItemDto> Items { get; set; } = new();
}

// DTO representing a single item in the order
public class OrderItemDto
{
    public int ProductId { get; set; }  // Product identifier
    public int Quantity { get; set; }   // Quantity requested
}