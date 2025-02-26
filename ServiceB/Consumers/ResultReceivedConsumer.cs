using DotNetCore.CAP;
using Shared.Events;

namespace ServiceB.Consumers;

public class ResultReceivedConsumer:ICapSubscribe
{
    private static readonly string FileStoragePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "FileStorage", "data.txt");

    [CapSubscribe("new-number-added")]
    public async Task Consume(NewNumberAddedEvent message)
    {
        var receivedNumber = message.Result;
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