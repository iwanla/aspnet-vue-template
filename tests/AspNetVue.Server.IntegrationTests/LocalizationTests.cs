using System.Globalization;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using AspNetVue.Server.Common.Endpoints;
using AspNetVue.Server.Features.Identity;

namespace AspNetVue.Server.IntegrationTests;

// The probe is registered only in the test host; no production route is added.
public sealed class LocalizationTests
{
    [Theory]
    [InlineData(null, "en", "Welcome!")]
    [InlineData("id-ID", "id", "Selamat datang!")]
    [InlineData("en-US", "en", "Welcome!")]
    [InlineData("fr-FR", "en", "Welcome!")]
    [InlineData("en;q=0.5,id;q=0.9", "id", "Selamat datang!")]
    public async Task Request_Should_SelectAcceptLanguageOrDefault(
        string? acceptLanguage, string expectedCulture, string expectedText)
    {
        await using var factory = new LocalizationFactory();
        using HttpClient client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/localization-probe?culture=fr");
        if (acceptLanguage is not null)
        {
            request.Headers.Add("Accept-Language", acceptLanguage);
        }

        using HttpResponseMessage response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        ProbeResponse result = (await response.Content.ReadFromJsonAsync<ProbeResponse>())!;

        result.Culture.ShouldBe(expectedCulture);
        result.UICulture.ShouldBe(expectedCulture);
        result.Text.ShouldBe(expectedText);
    }

    [Fact]
    public async Task ConcurrentRequests_Should_NotShareCultureState()
    {
        await using var factory = new LocalizationFactory();
        using HttpClient client = factory.CreateClient();

        await Task.WhenAll(Enumerable.Range(0, 20).Select(async index =>
        {
            string culture = index % 2 == 0 ? "en" : "id";
            using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/localization-probe");
            request.Headers.Add("Accept-Language", culture);
            using HttpResponseMessage response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            ProbeResponse result = (await response.Content.ReadFromJsonAsync<ProbeResponse>())!;

            result.Culture.ShouldBe(culture);
            result.Text.ShouldBe(culture == "en" ? "Welcome!" : "Selamat datang!");
        }));
    }

    private sealed class LocalizationFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("ConnectionStrings:Database", "Host=localhost;Database=localization");
            builder.UseSetting("Jwt:Secret", "localization-tests-secret-at-least-thirty-two-characters");
            builder.UseSetting("Jwt:Issuer", "localization-tests");
            builder.UseSetting("Jwt:Audience", "localization-tests");
            builder.ConfigureServices(services => services.AddSingleton<IEndpoint, ProbeEndpoint>());
        }
    }

    private sealed class ProbeEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("localization-probe", (IStringLocalizer<UserRegisteredDomainEvent> localizer) =>
                Results.Ok(new ProbeResponse(
                    CultureInfo.CurrentCulture.Name,
                    CultureInfo.CurrentUICulture.Name,
                    localizer["identity.registration.welcomeSubject"])))
                .MapToApiVersion(1);
        }
    }

    private sealed record ProbeResponse(string Culture, string UICulture, string Text);
}
