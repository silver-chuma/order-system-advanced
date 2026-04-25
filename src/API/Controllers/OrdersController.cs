using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Application;
using API.DTOs;
using System.Linq;

[ApiController]
[Route("api/orders")]
public class OrdersController : ControllerBase
{
    private readonly OrderService _s;

    public OrdersController(OrderService s)
    {
        _s = s;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateOrderRequest r,
        [FromHeader(Name = "Idempotency-Key")] string key)
    {
        // Optional validation (recommended)
        if (r.Items == null || !r.Items.Any())
            return BadRequest("Order must contain at least one item");

        var items = r.Items
            .Select(i => (i.ProductId, i.Quantity))
            .ToList();

        var id = await _s.PlaceOrder(items, key);

        return Ok(new { orderId = id });
    }
}