using Microsoft.EntityFrameworkCore;
using Domain;

namespace Infra;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> o) : base(o) {}

    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Enable optimistic concurrency control on Product
        modelBuilder.Entity<Product>()
        .Property(p => p.RowVersion)
        .IsConcurrencyToken();

        /*
         WHY THIS IS IMPORTANT:
         - RowVersion acts as a concurrency token
         - EF Core uses it to detect if a record has been modified by another request

         WHAT HAPPENS UNDER THE HOOD:
         - When updating a Product, EF includes RowVersion in the WHERE clause
         - If another request already modified the row:
             → RowVersion changes
             → Update affects 0 rows
             → EF throws DbUpdateConcurrencyException

         HOW WE HANDLE IT:
         - Polly catches the exception
         - Retries the operation with fresh data

         RESULT:
         - Prevents race conditions
         - Prevents overselling
        */

        // Ensure idempotency key is unique (prevents duplicate orders)
        modelBuilder.Entity<Order>()
            .HasIndex(o => o.IdempotencyKey)
            .IsUnique();
    }
}