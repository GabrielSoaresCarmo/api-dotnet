using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SQLitePCL;

var builder = WebApplication.CreateBuilder(args);

var jwtConfig = builder.Configuration.GetSection("Jwt");
var key = Encoding.UTF8.GetBytes(jwtConfig["Key"]!);

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

//Configure conection with database
builder.Services.AddSqlite<OrderDbContext>("Data source=database.db");

//Configure JWT authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtConfig["Issuer"],
        ValidAudience = jwtConfig["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
});
builder.Services.AddAuthorization();
builder.Services.AddScoped<TokenService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthentication();
app.UseAuthorization();

//Create a database file before recieve requests
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

app.MapPost("/login", async(Login dto, TokenService tokenService) =>
{
    try
    {
        if (dto.email == "admin@email.com" && dto.password == "12345")
        {
            var token = tokenService.GenerateToken("1", dto.email);
            return Results.Ok(new { token });
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Erro ao gerar o token: {ex.Message}");
        throw;
    }
    return Results.Unauthorized();
});

app.MapPost("/order", async(Order order, OrderDbContext db) =>
{
    try
    {   
        var exist = await db.Orders.AnyAsync(o => o.OrderId == order.OrderId);
        if (exist)
        {
            return Results.Conflict($"Order {order.OrderId} já existe");
        }
        await db.Orders.AddAsync(order);
        await db.SaveChangesAsync();
        return Results.Created("deu certo", order);
    }
    catch (System.Exception ex)
    {
        return Results.BadRequest(ex.Message);
        throw;
    }
}).RequireAuthorization();

app.Run();

public class OrderDbContext : DbContext 
{
    public OrderDbContext(DbContextOptions<OrderDbContext> options) : base(options) { }
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<Item> Items => Set<Item>();
}

public class Order
{
    [JsonPropertyName("numeroPedido")]
    public string OrderId { get; set; } = string.Empty;
    [JsonPropertyName("valorTotal")]
    public float Value { get; set; }
    [JsonPropertyName("dataCriacao")]
    public DateTime CreationDate { get; set; }
    public List<Item> Items { get; set; } = new();
}

public class Item
{
    [JsonPropertyName("idItem")]
    [Key]public int ProductId { get; set; }
    [JsonPropertyName("quantidadeItem")]
    public int Quantity { get; set; }
    [JsonPropertyName("valorItem")]
    public double Price { get; set; }
    public string OrderId { get; set; } = string.Empty;
}

record Login(string email, string password);