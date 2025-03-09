using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using ServiceA;
using Shared;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddOpenTelemetry()
    .WithTracing(tracerProviderBuilder =>
{
    tracerProviderBuilder
        .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(DiagnosticConfig.Client.Name))
        .AddSource(MassTransit.Logging.DiagnosticHeaders.DefaultListenerName)
        .AddAspNetCoreInstrumentation()  // For incoming HTTP/gRPC requests
        .AddGrpcClientInstrumentation()
        .AddSqlClientInstrumentation()// For database tracing
        .AddOtlpExporter();
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddGrpcClient<CalculatorService.CalculatorServiceClient>(o =>
    {
        o.Address = new Uri("http://service_a:8080"); // Ensure HTTP is used
    })
    .ConfigurePrimaryHttpMessageHandler(() =>
        new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        });
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};
app.MapPost("/calculator/add", async ([FromBody]AddCommand command,CalculatorService.CalculatorServiceClient client) =>
{
    using Activity? activity = DiagnosticConfig.Client.StartActivity($"{nameof(client)} send to add two number");
    activity?.AddTag("client-send-add-numbers", nameof(command));
    activity?.AddTag("Number1", command.Number1);
    activity?.AddTag("Number2", command.Number2);
    var request = new AddRequest { Number1 = command.Number1, Number2 = command.Number2 };
    var response = await client.AddAsync(request);
    return response.Result;
});


app.MapGet("/weatherforecast", () =>
    {
        var forecast = Enumerable.Range(1, 5).Select(index =>
                new WeatherForecast
                (
                    DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                    Random.Shared.Next(-20, 55),
                    summaries[Random.Shared.Next(summaries.Length)]
                ))
            .ToArray();
        return forecast;
    })
    .WithName("GetWeatherForecast")
    .WithOpenApi();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
record AddCommand(int Number1,int Number2);