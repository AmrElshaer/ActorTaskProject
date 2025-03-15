using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace ServiceA;

public class ServiceAdbContext(DbContextOptions<ServiceAdbContext> options) : DbContext(options)
{
    public DbSet<Entities.Message> Messages { get; set; }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        // Configure MassTransit OutBox Entities
        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();
    }
}