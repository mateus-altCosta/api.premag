using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Premag.Infrastructure.Tenancy;

namespace Premag.Infrastructure.Data;

public sealed class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var cs = Environment.GetEnvironmentVariable("PREMAG_CONNECTION")
            ?? "Host=localhost;Port=5432;Database=premag;Username=premag;Password=premag_dev";

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(cs)
            .UseSnakeCaseNamingConvention()
            .Options;

        return new ApplicationDbContext(options, new TenantContext());
    }
}
