using MassTransit;
using MassTransit.Logging;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using ServiceA;
using ServiceA.Services;
using Shared;
using CalculatorService = ServiceA.Services.CalculatorService;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenTelemetry()
    .WithTracing(tracerProviderBuilder =>
    {
        tracerProviderBuilder
            .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(DiagnosticConfig.ServiceA.Name))
            .AddSource(DiagnosticConfig.ServiceA.Name)
            .AddSource(DiagnosticHeaders.DefaultListenerName)
            .AddAspNetCoreInstrumentation()  // For incoming HTTP/gRPC requests
            .AddGrpcClientInstrumentation()
            .AddSqlClientInstrumentation()  // For database tracing
            .AddCapInstrumentation()
            .AddOtlpExporter();
    });
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<ServiceAdbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddCap(options =>
{
    options.UseSqlServer(connectionString!); // Store messages in DB
    options.UseRabbitMQ(rabbitMqOptions =>
    {
        rabbitMqOptions.HostName = "rabbitmq"; // Replace with the correct hostname or IP
        rabbitMqOptions.Port = 5672;           // Default RabbitMQ port
        rabbitMqOptions.UserName = "guest";    // Default username
        rabbitMqOptions.Password = "guest";    // Default password
    }); // Use RabbitMQ as transport
    options.UseDashboard(); // Optional: CAP Dashboard
});

// Add MassTransit with RabbitMQ
builder.Services.AddMassTransit(x =>
{
    x.SetKebabCaseEndpointNameFormatter();
    x.AddEntityFrameworkOutbox<ServiceAdbContext>(o =>
    {
        o.UseSqlServer().UseBusOutbox();
    });
    x.AddConfigureEndpointsCallback((context, name, cfg) => { cfg.UseEntityFrameworkOutbox<ServiceAdbContext>(context); });
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(new Uri("amqp://rabbitmq:5672"), h =>
        {
            h.Username("guest");
            h.Password("guest");
        });
        cfg.ConfigureEndpoints(context);
    });
});
// Add services to the container.
builder.Services.AddGrpc();

var app = builder.Build();
// Configure the HTTP request pipeline.
app.MapGrpcService<GreeterService>();
app.MapGrpcService<CalculatorService>();
app.MapGet("/",
    () =>
        "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");
// Enable middleware for routing
app.UseRouting();


app.UseHttpsRedirection();

app.Run();