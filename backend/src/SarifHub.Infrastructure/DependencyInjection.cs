using Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using SarifHub.Application.Common;
using SarifHub.Application.Dashboard;
using SarifHub.Application.Findings;
using SarifHub.Application.Projects;
using SarifHub.Application.Scans;
using SarifHub.Application.Tools;
using SarifHub.Application.Trends;
using SarifHub.Infrastructure.Persistence;
using SarifHub.Infrastructure.ReadModels;
using SarifHub.Infrastructure.Seeding;

namespace SarifHub.Infrastructure;

/// <summary>Registers the PostgreSQL-backed implementations of the Application ports.</summary>
public static class DependencyInjection
{
    /// <summary>Name of the connection string in configuration (<c>ConnectionStrings:SarifHub</c>).</summary>
    public const string ConnectionStringName = "SarifHub";

    /// <summary>Adds the database, the read models and the development seed.</summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        // Never echo the value: it contains a password.
        var connectionString = configuration.GetConnectionString(ConnectionStringName);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Connection string '{ConnectionStringName}' is not configured. Set it with " +
                $"'dotnet user-secrets set \"ConnectionStrings:{ConnectionStringName}\" \"<connection string>\" --project src/SarifHub.Api' " +
                $"or the environment variable 'ConnectionStrings__{ConnectionStringName}' (see README).");
        }

        // One data source (connection pool) shared by EF Core and Dapper.
        services.AddSingleton(_ => new NpgsqlDataSourceBuilder(connectionString).Build());
        services.AddDbContext<SarifHubDbContext>((provider, options) => options
            .UseNpgsql(provider.GetRequiredService<NpgsqlDataSource>())
            .UseSnakeCaseNamingConvention());

        // Dapper: map snake_case columns to PascalCase properties of the row types.
        DefaultTypeMap.MatchNamesWithUnderscores = true;

        services.AddScoped<IProjectAccess, ProjectAccess>();
        services.AddScoped<IProjectQueries, ProjectQueries>();
        services.AddScoped<IDashboardQuery, DashboardQuery>();
        services.AddScoped<IScanQueries, ScanQueries>();
        services.AddScoped<IFindingGridQuery, FindingGridQuery>();
        services.AddScoped<IFindingDetailQuery, FindingDetailQuery>();
        services.AddScoped<ITrendQuery, TrendQuery>();
        services.AddScoped<IToolQuery, ToolQuery>();
        services.AddScoped<DevelopmentDataSeeder>();

        services.AddHealthChecks().AddDbContextCheck<SarifHubDbContext>("database");
        return services;
    }
}
