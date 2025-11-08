using System.Reflection;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using InvoiceService.Infrastructure.Data;
using Shared.Contracts.Inventory;

var builder = WebApplication.CreateBuilder(args);

// Configuração do Entity Framework com PostgreSQL
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<InvoiceDbContext>(options =>
    options.UseNpgsql(connectionString));

// Configuração do MassTransit com RabbitMQ
builder.Services.AddMassTransit(x =>
{
    // Registra todos os Consumers automaticamente do assembly
    x.AddConsumers(Assembly.GetExecutingAssembly());

    // Registra Request Clients (apenas para operações que precisam de resposta)
    x.AddRequestClient<ConfirmReservationRequest>();

    x.UsingRabbitMq((context, cfg) =>
    {
        // Configuração do RabbitMQ
        cfg.Host(builder.Configuration.GetValue<string>("RabbitMQ:Host") ?? "localhost",
            builder.Configuration.GetValue<ushort>("RabbitMQ:Port", 5672), "/", h =>
            {
                h.Username(builder.Configuration.GetValue<string>("RabbitMQ:Username") ?? "guest");
                h.Password(builder.Configuration.GetValue<string>("RabbitMQ:Password") ?? "guest");
            });

        // timeout para Request/Response
        cfg.UseMessageRetry(r => r.Immediate(3));

        // endpoints automaticamente baseado nos Consumers
        cfg.ConfigureEndpoints(context);
    });
});

// Health checks
builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString!)
    .AddRabbitMQ(name: "rabbitmq");

var app = builder.Build();

// Aplica migrations automaticamente em desenvolvimento
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<InvoiceDbContext>();
    await dbContext.Database.MigrateAsync();
}

// Health check endpoint
app.MapHealthChecks("/health");

app.Run();
