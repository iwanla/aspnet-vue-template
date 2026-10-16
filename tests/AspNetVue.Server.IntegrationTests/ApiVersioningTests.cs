using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace AspNetVue.Server.IntegrationTests;

// Routing and documentation tests do not require a database or Docker.
public sealed class ApiVersioningTests
{
    private sealed class RoutingFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("ConnectionStrings:Database", "Host=localhost;Database=versioning");
            builder.UseSetting("Jwt:Secret", "versioning-tests-secret-at-least-thirty-two-characters");
            builder.UseSetting("Jwt:Issuer", "versioning-tests");
            builder.UseSetting("Jwt:Audience", "versioning-tests");
            builder.ConfigureServices(services =>
                services.PostConfigure<HealthCheckServiceOptions>(options => options.Registrations.Clear()));
        }
    }

    [Theory]
    [InlineData("/api/v1/todos", HttpStatusCode.Unauthorized)]
    [InlineData("/api/todos", HttpStatusCode.NotFound)]
    [InlineData("/api/v2/todos", HttpStatusCode.NotFound)]
    [InlineData("/api/v1/health", HttpStatusCode.NotFound)]
    [InlineData("/api/health", HttpStatusCode.OK)]
    public async Task Routes_Should_EnforceVersionedBusinessApi_AndNeutralHealth(
        string path, HttpStatusCode expected)
    {
        await using var factory = new RoutingFactory();
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client.GetAsync(path);

        response.StatusCode.ShouldBe(expected);
    }

    [Fact]
    public async Task OpenApi_Should_SubstituteVersionInAllBusinessPaths()
    {
        await using var factory = new RoutingFactory();
        using HttpClient client = factory.CreateClient();
        using HttpResponseMessage response = await client.GetAsync("/openapi/v1.json");
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        JsonElement paths = document.RootElement.GetProperty("paths");
        paths.TryGetProperty("/api/v1/todos", out _).ShouldBeTrue();
        paths.TryGetProperty("/api/v1/users/register", out _).ShouldBeTrue();
        foreach (JsonProperty path in paths.EnumerateObject())
        {
            path.Name.StartsWith("/api/v1/", StringComparison.Ordinal).ShouldBeTrue();
            path.Name.Contains("{version}", StringComparison.Ordinal).ShouldBeFalse();
        }
    }
}
