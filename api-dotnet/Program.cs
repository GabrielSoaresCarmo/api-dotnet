using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddSqlite<OrderDbContext>("Data source=database.db");

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

//
using (var scope = app.Services.CreateScope())
{
    try 
    {
        var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
        db.Database.EnsureCreated();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Erro ao criar o banco: {ex.Message}");
    }
}

app.UseHttpsRedirection();

app.MapGet("/", () => "Welcome to .NET API learn");

app.Run();

public class OrderDbContext : DbContext 
{
    public OrderDbContext(DbContextOptions<OrderDbContext> options) : base(options) { }
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<Item> Items => Set<Item>();
}

public class Order
{
    public required string OrderId { get; set; }
    public float Value { get; set; }
    public DateOnly CreationDate { get; set; }
    public List<Item> Items { get; set; } = new();
}

public class Item
{
    [Key]public int ProductId { get; set; }
    public int Quantity { get; set; }
    public double Price { get; set; }
    public required string OrderId { get; set; }
}