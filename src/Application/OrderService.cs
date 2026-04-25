using Domain;
using Infra;
using Microsoft.EntityFrameworkCore;
using Polly;

namespace Application;


// This service handles order placement with concurrency safety.
//
// Concurrency strategy:
// - Uses optimistic concurrency via RowVersion (configured in Product entity)
// - Wraps updates in a database transaction for atomicity
// - Uses Polly retry policy to handle DbUpdateConcurrencyException
// - Each retry uses a fresh DbContext to avoid stale entity tracking
//
// Goal:
// - Prevent overselling under concurrent requests
// - Ensure consistent stock updates

public class OrderService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    private readonly EventBus _bus;

    public OrderService(IDbContextFactory<AppDbContext> dbFactory, EventBus bus)
    {
        _dbFactory = dbFactory;
        _bus = bus;
    }

    public async Task<int> PlaceOrder(List<(int productId, int quantity)> items, string idempotencyKey)
    {
        // 🔁 Retry policy for handling concurrency conflicts
        var retryPolicy = Policy
            .Handle<DbUpdateConcurrencyException>()
            .WaitAndRetryAsync(3, retryAttempt =>
            {
                Console.WriteLine($"[RETRY] Concurrency conflict. Attempt {retryAttempt}");
                return TimeSpan.FromMilliseconds(150);
            });

        return await retryPolicy.ExecuteAsync(async () =>
        {
            // Create fresh DbContext for each retry attempt
            await using var db = _dbFactory.CreateDbContext();

            // Idempotency check to prevent duplicate orders
            var existingOrder = await db.Orders
                .FirstOrDefaultAsync(o => o.IdempotencyKey == idempotencyKey);

            if (existingOrder != null)
                return existingOrder.Id;

            //  Begin transaction to ensure atomic operation
            using var transaction = await db.Database.BeginTransactionAsync();

            try
            {
                foreach (var item in items)
                {
                    // Fetch latest product state from DB
                    var product = await db.Products
                        .FirstOrDefaultAsync(p => p.Id == item.productId);

                    if (product == null)
                        throw new Exception($"Product {item.productId} not found");

                    // ⚠️ Validate stock availability
                    if (product.Stock < item.quantity)
                        throw new Exception($"Insufficient stock for product {item.productId}");

                    // Deduct stock
                    product.Stock -= item.quantity;
                }

                // Create order record
                var order = new Order
                {
                    IdempotencyKey = idempotencyKey,
                    CreatedAt = DateTime.UtcNow
                };

                db.Orders.Add(order);

                // CRITICAL POINT:
                // SaveChangesAsync is where EF Core checks RowVersion.
                // If another request modified the same product:
                // EF throws DbUpdateConcurrencyException
                // Polly retry policy re-executes the operation
                await db.SaveChangesAsync();

                // Commit transaction only after successful save
                await transaction.CommitAsync();

                // Simulate event flow
                await _bus.Publish("OrderPlaced", order.Id);
                Console.WriteLine("[PAYMENT] Processing...");

                // Simulate occasional payment failure
                if (new Random().Next(0, 4) == 1)
                    throw new Exception("Simulated payment failure");

                Console.WriteLine("[PAYMENT] Success");
                Console.WriteLine("[NOTIFICATION] Email sent");

                return order.Id;
            }
            catch
            {
                // Rollback on any failure
                await transaction.RollbackAsync();
                throw;
            }
        });
    }
}