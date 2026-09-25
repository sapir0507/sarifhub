using SarifHub.Application.Common;
using SarifHub.Application.Dashboard;
using SarifHub.Application.Findings;
using SarifHub.Application.Projects;
using SarifHub.Application.Scans;
using SarifHub.Application.Tools;
using SarifHub.Application.Trends;
using SarifHub.Infrastructure.Development;

namespace SarifHub.Api.Infrastructure;

/// <summary>Composition root helpers.</summary>
internal static class ServiceCollectionExtensions
{
    /// <summary>Registers the Application use cases and the caller identity.</summary>
    public static IServiceCollection AddSarifHubApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(TimeProvider.System);

        services.AddScoped<ProjectReadAccess>();
        services.AddScoped<ListProjects>();
        services.AddScoped<GetDashboard>();
        services.AddScoped<ListScans>();
        services.AddScoped<GetScan>();
        services.AddScoped<SearchFindings>();
        services.AddScoped<GetFinding>();
        services.AddScoped<GetTrends>();
        services.AddScoped<ListTools>();

        // ADR 0011: until Phase 6, every request acts as the configured seeded user. Replaced by the authenticated user.
        services.Configure<DevelopmentOptions>(configuration.GetSection(DevelopmentOptions.SectionName));
        services.AddScoped<ICurrentUser, DevelopmentCurrentUser>();
        return services;
    }
}
