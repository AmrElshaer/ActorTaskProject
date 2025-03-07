using MassTransit;
using Microsoft.EntityFrameworkCore;
using ServiceA;
using ServiceA.Services;
using CalculatorService = ServiceA.Services.CalculatorService;

var builder = WebApplication.CreateBuilder(args);

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
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(new Uri("amqp://rabbitmq:5672"), h =>
        {
            h.Username("guest");
            h.Password("guest");
        });
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