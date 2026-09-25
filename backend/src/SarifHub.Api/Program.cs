using System.Text.Json.Serialization;
using SarifHub.Api.Infrastructure;
using SarifHub.Infrastructure;
using SarifHub.Infrastructure.Seeding;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// ADR 0011: authentication arrives in Phase 6. Until then the API must never run outside Development.
if (!builder.Environment.IsDevelopment())
{
    throw new InvalidOperationException(
        "SarifHub has no authentication yet (Phase 6) and only runs in the Development environment. See ADR 0011.");
}

builder.Services
    .AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// RFC 9457 Problem Details for every error; exception details stay in the server log.
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<DatabaseUnavailableExceptionHandler>();

builder.Services.AddOpenApi(options => options.AddDocumentTransformer((document, _, _) =>
{
    document.Info.Title = "SarifHub API";
    document.Info.Version = "v1";
    document.Info.Description =
        "Security findings from SARIF files, tracked across scans. Phase 3: read-only endpoints, " +
        "Development only, no authentication yet (requests act as the seeded demo user).";
    return Task.CompletedTask;
}));

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddSarifHubApplication(builder.Configuration);
builder.Services.AddFrontendCors(builder.Configuration);

var app = builder.Build();

// `dotnet run --project src/SarifHub.Api -- seed` fills an empty database with demo data and exits.
if (args is ["seed"])
{
    await using var scope = app.Services.CreateAsyncScope();
    var result = await scope.ServiceProvider.GetRequiredService<DevelopmentDataSeeder>().SeedAsync(CancellationToken.None);
    return result == SeedResult.MigrationsPending ? 1 : 0;
}

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();                 // /openapi/v1.json
    app.MapScalarApiReference();      // /scalar
}

app.UseCors(CorsSetup.FrontendPolicy);
app.MapControllers();
app.MapHealthChecks("/health");

await app.RunAsync();
return 0;
