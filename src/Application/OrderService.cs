using Domain;
using Infra;
using Microsoft.EntityFrameworkCore;
using Polly;
using Application.Exceptions;
using Application.Models;

namespace Application;

// This service handles order placement with concurrency safety.
//
// Concurrency strategy:
// - Uses optimistic concurrency via RowVersion (configured in Product entity)
// - Wraps database updates in a transaction for atomicity
// - Uses Polly retry policy to handle DbUpdateConcurrencyException
// - Each retry uses a fresh DbContext to avoid stale entity tracking
//
// Important design:
// - Database transaction is strictly limited to data persistence
// - External side effects (events, payment simulation, notifications)
//   are executed AFTER the transaction commits
//
// Goal:
// - Prevent overselling under concurrent requests
// - Ensure consistent stock updates
// - Avoid rolling back committed transactions due to downstream failures

public class OrderService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly EventBus _bus;

    public OrderService(IDbContextFactory<AppDbContext> dbFactory, EventBus bus)
    {
        _dbFactory = dbFactory;
        _bus = bus;
    }

    public async Task<OrderResult> PlaceOrder(List<(int productId, int quantity)> items, string idempotencyKey)
    {
        var retryPolicy = Policy
            .Handle<DbUpdateConcurrencyException>()
            .WaitAndRetryAsync(3, retryAttempt =>
            {
                Console.WriteLine($"[RETRY] Concurrency conflict. Attempt {retryAttempt}");
                return TimeSpan.FromMilliseconds(150);
            });

        return await retryPolicy.ExecuteAsync(async () =>
        {
            await using var db = _dbFactory.CreateDbContext();

            // Idempotency check
            var existingOrder = await db.Orders
                .FirstOrDefaultAsync(o => o.IdempotencyKey == idempotencyKey);

            if (existingOrder != null)
            {
                return new OrderResult
                {
                    OrderId = existingOrder.Id,
                    RemainingStock = new Dictionary<int, int>()
                };
            }

            var remainingStock = new Dictionary<int, int>();
            int orderId;

            using var transaction = await db.Database.BeginTransactionAsync();

            try
            {
                foreach (var item in items)
                {
                    var product = await db.Products.FindAsync(item.productId);

                    if (product == null)
                        throw new Exception($"Product {item.productId} not found");

                    if (product.Stock < item.quantity)
                        throw new BusinessException(
                            $"Insufficient stock for product '{product.Name}' (ID: {product.Id}). Please restock."
                        );

                    // Deduct stock
                    product.Stock -= item.quantity;

                    // Track remaining stock
                    remainingStock[product.Id] = product.Stock;
                }

                var order = new Order
                {
                    IdempotencyKey = idempotencyKey,
                    CreatedAt = DateTime.UtcNow
                };

                db.Orders.Add(order);

                // CRITICAL POINT:
                // SaveChangesAsync is where EF Core checks RowVersion.
                // If another request modified the same product:
                // → EF throws DbUpdateConcurrencyException
                // → Polly retries with a fresh DbContext

                await db.SaveChangesAsync();

                await transaction.CommitAsync();

                orderId = order.Id;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }

            // Post-transaction (side effects)
            try
            {

                // IMPORTANT:
                // External side effects are executed AFTER transaction commit.
                // This ensures:
                // - No rollback is attempted on a completed transaction
                // - Database consistency is preserved
                // - System remains resilient under partial failures
                await _bus.Publish("OrderPlaced", orderId);

                Console.WriteLine("[PAYMENT] Processing...");

                if (new Random().Next(0, 4) == 1)
                    throw new Exception("Simulated payment failure");

                Console.WriteLine("[PAYMENT] Success");
                Console.WriteLine("[NOTIFICATION] Email sent");
            }
            catch (Exception ex)
            {
                // Do NOT fail order if side effects fail
                Console.WriteLine($"[ERROR] Post-order processing failed: {ex.Message}");
            }

            return new OrderResult
            {
                OrderId = orderId,
                RemainingStock = remainingStock
            };
        });
    }
}