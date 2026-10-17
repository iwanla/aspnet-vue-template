using Microsoft.EntityFrameworkCore;
using AspNetVue.Server.Database;

namespace AspNetVue.Server.Authorization;

/// <summary>
/// Applies migrations (development only) and seeds the permission catalog at startup. Seeding is
/// idempotent. When it fails the application still starts and permission checks deny access, which
/// is the fail-closed behavior we want.
/// </summary>
internal sealed class AuthorizationDataInitializer(
    IServiceScopeFactory serviceScopeFactory,
    IHostEnvironment environment,
    ILogger<AuthorizationDataInitializer> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using IServiceScope scope = serviceScopeFactory.CreateScope();

        try
        {
            ApplicationDbContext db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            if (environment.IsDevelopment())
            {
                await db.Database.MigrateAsync(cancellationToken);
            }

            AuthorizationSeeder seeder = scope.ServiceProvider.GetRequiredService<AuthorizationSeeder>();

            await seeder.SeedAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Authorization seeding failed. Permission checks will deny access until the database is seeded.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
