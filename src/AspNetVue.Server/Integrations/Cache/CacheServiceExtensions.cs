using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AspNetVue.Server.Integrations.Cache;

public static class CacheServiceExtensions
{
    /// <summary>
    /// HybridCache with configurable defaults. Default is in-memory L1 only;
    /// set Cache:Provider to Redis (plus a connection string) to add a distributed L2.
    /// </summary>
    public static IServiceCollection AddCacheIntegration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        CacheOptions options = configuration
            .GetSection(CacheOptions.SectionName)
            .Get<CacheOptions>() ?? new CacheOptions();

        int expirationMinutes = options.DefaultExpirationMinutes <= 0
            ? 5
            : options.DefaultExpirationMinutes;
        var expiration = TimeSpan.FromMinutes(expirationMinutes);

#pragma warning disable EXTEXP0018 // HybridCache is released; the API is stable in .NET 10.
        services.AddHybridCache(hybrid =>
        {
            hybrid.DefaultEntryOptions = new HybridCacheEntryOptions
            {
                Expiration = expiration,
                LocalCacheExpiration = expiration
            };
        });
#pragma warning restore EXTEXP0018

        if (options.Provider.Equals("Redis", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(options.Redis.ConnectionString))
            {
                throw new InvalidOperationException(
                    "Cache:Provider is 'Redis' but Cache:Redis:ConnectionString is empty.");
            }

            string prefix = string.IsNullOrWhiteSpace(options.KeyPrefix)
                ? "aspnetvue:"
                : options.KeyPrefix.TrimEnd(':') + ":";

            services.AddStackExchangeRedisCache(redis =>
            {
                redis.Configuration = options.Redis.ConnectionString;
                redis.InstanceName = prefix;
            });
        }

        return services;
    }
}
