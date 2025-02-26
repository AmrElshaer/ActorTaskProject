using Microsoft.EntityFrameworkCore;

namespace ServiceA;

public class ServiceAdbContext(DbContextOptions<ServiceAdbContext> options) : DbContext(options)
{
    public DbSet<Entities.Message> Messages { get; set; }
}