using CatalogService.Data;
using CatalogService.GrpcServices;
using CatalogService.Protos;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddDbContext<CatalogDbContext>(options =>
{
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("CatalogDb"),
        npgsqlOptions =>
        {
            npgsqlOptions.MigrationsAssembly(typeof(CatalogDbContext).Assembly.FullName);
            npgsqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorCodesToAdd: null);
        });
});

builder.Services.AddSingleton<StackExchange.Redis.IConnectionMultiplexer>(sp =>
{
    var connectionString = builder.Configuration.GetConnectionString("Redis")
        ?? "localhost:6379";
    return StackExchange.Redis.ConnectionMultiplexer.Connect(connectionString);
});

builder.Services.AddGrpc();

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService("CatalogService", serviceVersion: "1.0.0"))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddEntityFrameworkCoreInstrumentation()
        .AddOtlpExporter(options =>
        {
            options.Endpoint = new Uri(
                builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]
                ?? "http://localhost:4317");
        }));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Development-only middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();       // Enable Swagger UI at /swagger
    app.UseSwaggerUI();     // Interactive API documentation
}

// Global exception handling (returns 500 with error details in dev)
app.UseExceptionHandler("/error");

// CORS middleware
app.UseCors();

// Map controllers
app.MapControllers();

// Map gRPC services
app.MapGrpcService<InventoryGrpcServiceImpl>();

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    service = "CatalogService",
    timestamp = DateTime.UtcNow
}));

// Seed sample data on startup (development only)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
    await db.Database.EnsureCreatedAsync(); 
    await SeedDataAsync(db);
}

Log.Information("CatalogService starting...");
app.Run();

static async Task SeedDataAsync(CatalogDbContext db)
{
    if (await db.Products.AnyAsync())
        return;  // Already seeded

    var products = new[]
    {
        new CatalogService.Data.Entities.Product
        {
            Id = Guid.Parse("f9dd4662-6ed2-4a64-a52a-2db20c7bee72"),
            Name = "Wireless Mouse",
            Description = "Ergonomic wireless mouse with USB receiver",
            Price = 29.99m,
            Category = "Electronics",
            StockQuantity = 150
        },
        new CatalogService.Data.Entities.Product
        {
            Id = Guid.Parse("b1c2d3e4-f5a6-7890-bcde-f12345678901"),
            Name = "Mechanical Keyboard",
            Description = "RGB mechanical keyboard with Cherry MX switches",
            Price = 89.99m,
            Category = "Electronics",
            StockQuantity = 75
        },
        new CatalogService.Data.Entities.Product
        {
            Id = Guid.Parse("c2d3e4f5-a6b7-8901-cdef-123456789012"),
            Name = "USB-C Hub",
            Description = "7-in-1 USB-C hub with HDMI, USB 3.0, and SD card reader",
            Price = 49.99m,
            Category = "Electronics",
            StockQuantity = 200
        },
        new CatalogService.Data.Entities.Product
        {
            Id = Guid.Parse("d3e4f5a6-b7c8-9012-def1-234567890123"),
            Name = "Cotton T-Shirt",
            Description = "Premium cotton t-shirt, available in multiple colors",
            Price = 19.99m,
            Category = "Clothing",
            StockQuantity = 500
        },
        new CatalogService.Data.Entities.Product
        {
            Id = Guid.Parse("e4f5a6b7-c8d9-0123-ef12-345678901234"),
            Name = "Running Shoes",
            Description = "Lightweight running shoes with cushioned sole",
            Price = 79.99m,
            Category = "Sports",
            StockQuantity = 100
        }
    };

    db.Products.AddRange(products);
    await db.SaveChangesAsync();

    Log.Information("Seeded {Count} sample products", products.Length);
}
