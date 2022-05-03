using Microsoft.EntityFrameworkCore;

namespace Demo.Service;

public class WeatherContext : DbContext
{
    public WeatherContext(DbContextOptions<WeatherContext> context) : base(context) { }

    public DbSet<WeatherServiceRequest> WeatherServiceRequests { get; set; }
}

public class WeatherServiceRequest
{
    public Guid Id { get; set; }
    public string Note { get; set; } = string.Empty;
}
