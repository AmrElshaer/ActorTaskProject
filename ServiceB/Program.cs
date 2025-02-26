using MassTransit;
using Microsoft.EntityFrameworkCore;
using ServiceB;
using ServiceB.Consumers;

var builder = WebApplication.CreateBuilder(args);

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
        rabbitMqOptions.HostName = "localhost"; // Replace with the correct hostname or IP
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
        cfg.Host("localhost", "/", h =>
        {
            h.Username("guest");
            h.Password("guest");
        });
    });
});


var app = builder.Build();
// Enable middleware for routing
app.UseRouting();


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
app.Run();