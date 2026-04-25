
using Infra;
using Application;
using Microsoft.EntityFrameworkCore;

var b=WebApplication.CreateBuilder(args);

b.Services.AddDbContext<AppDbContext>(o=>o.UseSqlite("Data Source=orders.db"));
b.Services.AddScoped<OrderService>();
b.Services.AddSingleton<EventBus>();
b.Services.AddHostedService<Worker>();
b.Services.AddControllers();
b.Services.AddSwaggerGen();

var app=b.Build();
app.UseSwagger();app.UseSwaggerUI();
app.MapControllers();
app.Run();
