using System.ComponentModel.DataAnnotations;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

var jwtConfig = builder.Configuration.GetSection("Jwt");
var key = Encoding.UTF8.GetBytes(jwtConfig["Key"]);

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddSqlite<OrderDbContext>("Data source=database.db");
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

//app.UseAuthentication();
//app.UseAuthorization();

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

app.MapPost("/login", (Login dto, TokenService tokenService) =>
{
    try
    {
        if (dto.Email == "admin@email.com" && dto.Password == "12345")
        {
            var token = tokenService.GenerateToken("1", dto.Email);
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

record Login(string Email, string Password);