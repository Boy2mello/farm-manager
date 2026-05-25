using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FarmManager.Infrastructure.BackgroundJobs;

public static class HangfireServiceCollectionExtensions
{
    public static IServiceCollection AddFarmHangfire(this IServiceCollection services, IConfiguration configuration)
    {
        var connection = configuration.GetConnectionString("Hangfire")
            ?? configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Hangfire connection string is required.");

        services.AddHangfire(cfg => cfg
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(opt => opt.UseNpgsqlConnection(connection)));

        return services;
    }

    public static IServiceCollection AddFarmHangfireServer(this IServiceCollection services)
    {
        services.AddHangfireServer(opts =>
        {
            opts.WorkerCount = Math.Max(2, Environment.ProcessorCount);
            opts.Queues = new[] { "default", "notifications", "analytics" };
            opts.SchedulePollingInterval = TimeSpan.FromSeconds(15);
        });
        return services;
    }
}
