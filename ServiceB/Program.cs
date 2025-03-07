using System.Diagnostics.Metrics;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Prometheus;
using ServiceB;
using ServiceB.Consumers;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddMeter("MyServiceMetrics") 
        .AddPrometheusExporter())
    .WithTracing(tracing =>
    {
        tracing.AddAspNetCoreInstrumentation()
            .AddGrpcClientInstrumentation()
            .AddConsoleExporter();
    });
builder.Services
    .AddEndpointsApiExplorer();
builder.Services
    .AddSwaggerGen();
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ServiceBdbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddTransient<ResultReceivedConsumer>();
builder.Services.AddCap(options =>
{
    options.UseSqlServer(connectionString!); // Store messages in DB
    options.UseRabbitMQ(rabbitMqOptions =>
    {
        rabbitMqOptions.HostName = "rabbitmq"; // Replace with the correct hostname or IP
        rabbitMqOptions.Port = 5672; // Default RabbitMQ port
        rabbitMqOptions.UserName = "guest"; // Default username
        rabbitMqOptions.Password = "guest"; // Default password
    }); // Use RabbitMQ as transport
    options.UseDashboard(); // Optional: CAP Dashboard
});

// Add MassTransit with RabbitMQ
builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(new Uri("amqp://rabbitmq:5672"), h =>
        {
            h.Username("guest");
            h.Password("guest");
        });
    });
});


var app = builder.Build();
app.UseOpenTelemetryPrometheusScrapingEndpoint();
// Enable middleware for routing
app.UseRouting();
// Expose Prometheus metrics endpoint


// Enable Swagger (Optional)
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.MapGet("get-current-number", async () =>
{
    var fileStoragePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "FileStorage", "data.txt");
    if (!File.Exists(fileStoragePath))
        await File.WriteAllTextAsync(fileStoragePath, "0"); // Initialize with 0 if the file is missing

    // Read the existing number from the file
    var existingText = await File.ReadAllTextAsync(fileStoragePath);
    if (!int.TryParse(existingText.Trim(),
            out var existingNumber)) existingNumber = 0; // Fallback if the file contains invalid data

    // Write the updated number back to the file

    return existingNumber;
});
app.UseEndpoints(endpoints =>
{
    endpoints.MapMetrics(); // Expose metrics at /metrics
    endpoints.MapControllers();
});
app.Run();