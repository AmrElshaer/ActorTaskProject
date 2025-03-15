using System.Diagnostics;
using DotNetCore.CAP;
using Grpc.Core;
using MassTransit;
using ServiceA.Entities;
using Shared;
using Shared.Events;

namespace ServiceA.Services;

public class CalculatorService(IPublishEndpoint publishEndpoint,ServiceAdbContext dbContext):ServiceA.CalculatorService.CalculatorServiceBase
{
    public override async Task<AddResponse> Add(AddRequest request, ServerCallContext context)
    {
        using Activity? activity = DiagnosticConfig.ServiceA.StartActivity("Publish result to queue");
        activity?.AddTag("publish calculate two type", nameof(request));
        activity?.AddTag("Number1", request.Number1);
        activity?.AddTag("Number2", request.Number2);
        var result = request.Number1 + request.Number2;
     
           var newMessage = new Message(request.Number1, request.Number2, DateTime.UtcNow);
           dbContext.Messages.Add(newMessage);
           await publishEndpoint.Publish(new NewNumberAddedEvent { Result = result});
           await dbContext.SaveChangesAsync();
         
       

        return new AddResponse
        {
            Result = result
        };
    }
}