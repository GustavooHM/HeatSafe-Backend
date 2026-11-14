using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace HeatSafe.Api.Data.DesignTime;

/// <summary>
/// Creates the EF Core context for migrations without starting the API,
/// hosted background services, or the runtime migration pipeline.
/// </summary>
public sealed class HeatSafeDbContextFactory : IDesignTimeDbContextFactory<HeatSafeDbContext>
{
    public HeatSafeDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<HeatSafeDbContext>()
            .UseSqlite("Data Source=heatsafe-design-time.db")
            .Options;

        return new HeatSafeDbContext(options);
    }
}
