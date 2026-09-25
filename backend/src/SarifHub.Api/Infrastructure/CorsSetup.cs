namespace SarifHub.Api.Infrastructure;

/// <summary>CORS for the Vite development server. Origins come from configuration (<c>Cors:AllowedOrigins</c>).</summary>
internal static class CorsSetup
{
    public const string FrontendPolicy = "Frontend";

    public static IServiceCollection AddFrontendCors(this IServiceCollection services, IConfiguration configuration)
    {
        var origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        return services.AddCors(options => options.AddPolicy(FrontendPolicy, policy => policy
            .WithOrigins(origins)
            .WithMethods("GET")
            .WithHeaders("Accept", "Content-Type")
            .WithExposedHeaders("ETag")));
    }
}
