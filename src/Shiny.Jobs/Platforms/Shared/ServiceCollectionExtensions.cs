#if PLATFORM
using Microsoft.Extensions.DependencyInjection;
using Shiny.Jobs;
using Shiny.Jobs.Infrastructure;

namespace Shiny;


public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddJob<TJob>(this IServiceCollection services) where TJob : class, IJob
    {
        services.AddShinyService<TJob>();
        services.AddJobs();

        return services;
    }


    public static IServiceCollection AddJobs(this IServiceCollection services)
    {
        // TODO: only add if not already regged
        services.AddShinyService<JobExecutor>();
        services.AddShinyService<JobTask>();
        services.AddShinyService<JobManager>();

        services.AddBattery();
        services.AddConnectivity();
        return services;
    }
}

#endif