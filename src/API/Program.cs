
using Infra;
using Application;
using Microsoft.EntityFrameworkCore;
using Domain;

var b=WebApplication.CreateBuilder(args);

b.Services.AddDbContextFactory<AppDbContext>(o =>
    o.UseSqlite("Data Source=orders.db"));
b.Services.AddScoped<OrderService>();
b.Services.AddSingleton<EventBus>();
b.Services.AddHostedService<Worker>();
b.Services.AddControllers();
b.Services.AddSwaggerGen();

var app=b.Build();
app.UseSwagger();app.UseSwaggerUI();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    
    var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
    using var db = factory.CreateDbContext();

    db.Database.EnsureCreated();

    if (!db.Products.Any())
    {
        db.Products.AddRange(
            new Product { Id = 1, Name = "T-Shirt", Stock = 200 },
            new Product { Id = 2, Name = "Jeans", Stock = 200 }
        );
        db.SaveChanges();
    }
}

app.Run();
