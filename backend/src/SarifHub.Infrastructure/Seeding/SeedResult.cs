namespace SarifHub.Infrastructure.Seeding;

/// <summary>Outcome of <see cref="DevelopmentDataSeeder.SeedAsync"/>.</summary>
public enum SeedResult
{
    /// <summary>Demo data was inserted.</summary>
    Seeded,

    /// <summary>The database already had projects; nothing was changed.</summary>
    AlreadySeeded,

    /// <summary>Migrations are pending; nothing was changed.</summary>
    MigrationsPending,
}
