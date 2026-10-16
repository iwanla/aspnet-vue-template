using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using AspNetVue.Server.Integrations.Cache;

namespace AspNetVue.Server.UnitTests.Cache;

public sealed class CacheServiceExtensionsTests
{
    [Fact]
    public void AddCacheIntegration_WithoutSection_RegistersHybridCache()
    {
        // Arrange
        var services = new ServiceCollection();
        IConfiguration configuration = new ConfigurationBuilder().Build();

        // Act
        services.AddCacheIntegration(configuration);

        // Assert
        using ServiceProvider provider = services.BuildServiceProvider();
        provider.GetRequiredService<HybridCache>().ShouldNotBeNull();
    }

    [Fact]
    public void AddCacheIntegration_RedisWithoutConnectionString_Throws()
    {
        // Arrange
        var services = new ServiceCollection();
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Cache:Provider"] = "Redis",
                ["Cache:Redis:ConnectionString"] = ""
            })
            .Build();

        // Act / Assert
        Should.Throw<InvalidOperationException>(() => services.AddCacheIntegration(configuration));
    }
}
