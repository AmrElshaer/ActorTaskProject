using System.Diagnostics;
using System.Diagnostics.Metrics;
using MassTransit;
using Shared;
using Shared.Events;

namespace ServiceB.Consumers;

public class ResultReceivedConsumer:IConsumer<NewNumberAddedEvent>
{
    private static readonly string FileStoragePath = Path.Combine("/app/FileStorage", "data.txt");
   
    private static readonly Meter Meter = new Meter("ServiceBMetrics");

    // Define a Histogram
    private static readonly Histogram<double> MessageProcessingTime = Meter.CreateHistogram<double>(
        "queue_latency_seconds",
        "seconds",
        "Time a message spends in the queue before being consumed"
    );
    public ResultReceivedConsumer()
    {
      
    }
    
    public async Task Consume(ConsumeContext<NewNumberAddedEvent> context)
    {
        var message = context.Message;
        using Activity? activity = DiagnosticConfig.ServiceB.StartActivity("consume message from queue");
        var consumeTime = DateTime.UtcNow;
        var latency = (consumeTime - message.CreationDate).TotalMilliseconds;
        MessageProcessingTime.Record(latency);
        var receivedNumber = message.Result;
        
        activity?.AddTag("consumer calculate two type", nameof(message));
        activity?.AddTag("result", message.Result);
        activity?.AddTag("creationDate", message.CreationDate);
        activity?.AddTag("total mill second latency ", latency);
        Console.WriteLine($"Received Number: {receivedNumber}");

        try
        {
            // Ensure the file exists and has a default value if empty
            if (!File.Exists(FileStoragePath))
            {
                await File.WriteAllTextAsync(FileStoragePath, "0"); // Initialize with 0 if the file is missing
            }

            // Read the existing number from the file
            string existingText = await File.ReadAllTextAsync(FileStoragePath);
            if (!int.TryParse(existingText.Trim(), out int existingNumber))
            {
                existingNumber = 0; // Fallback if the file contains invalid data
            }

            // Update the number (e.g., sum the values)
            int updatedNumber = existingNumber + receivedNumber;

            // Write the updated number back to the file
            await File.WriteAllTextAsync(FileStoragePath, updatedNumber.ToString());

            Console.WriteLine($"Updated number in {FileStoragePath}: {updatedNumber}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing number: {ex.Message}");
        }
    }
}