using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace ServiceB;

public class ServiceBdbContext(DbContextOptions<ServiceBdbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        // Configure MassTransit OutBox Entities
        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();
    }
}