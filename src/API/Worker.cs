using Microsoft.Extensions.Hosting;
using Application;

public class Worker : BackgroundService
{
    private readonly EventBus _bus;

    public Worker(EventBus bus)
    {
        _bus = bus;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Subscribe to OrderPlaced event
        _bus.Subscribe("OrderPlaced", async (data) =>
        {
            var orderId = (int)data;

            Console.WriteLine($"[EVENT RECEIVED] OrderPlaced: {orderId}");

            Console.WriteLine("[PAYMENT] Processing...");
            await Task.Delay(300);

            Console.WriteLine("[INVENTORY] Confirmed");
            Console.WriteLine("[NOTIFICATION] Email sent");
        });

        return Task.CompletedTask;
    }
}