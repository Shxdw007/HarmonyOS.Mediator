using Microsoft.EntityFrameworkCore;

namespace HarmonyOS.Mediator.Data;

public class AppDbContext : DbContext
{
    public DbSet<ToxicityIncident> Incidents { get; set; }

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
        Database.EnsureCreated(); 
    }
}