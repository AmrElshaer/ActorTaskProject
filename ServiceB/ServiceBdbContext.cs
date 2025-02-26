using Microsoft.EntityFrameworkCore;

namespace ServiceB;

public class ServiceBdbContext(DbContextOptions<ServiceBdbContext> options) : DbContext(options);