namespace Application.Models;

public class OrderResult
{
    public int OrderId { get; set; }

    // ProductId - Remaining stock
    public Dictionary<int, int> RemainingStock { get; set; } = new();
}