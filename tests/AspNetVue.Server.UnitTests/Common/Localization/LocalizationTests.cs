using System.Collections;
using System.Globalization;
using System.Resources;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using AspNetVue.Server.Common.Localization;
using AspNetVue.Server.Features.Identity;

namespace AspNetVue.Server.UnitTests.Common.Localization;

public sealed class LocalizationTests
{
    [Theory]
    [InlineData("en", "Welcome!")]
    [InlineData("en-US", "Welcome!")]
    [InlineData("id", "Selamat datang!")]
    [InlineData("id-ID", "Selamat datang!")]
    [InlineData("fr-FR", "Welcome!")]
    [InlineData("", "Welcome!")]
    public void Lookup_Should_UseCultureParentsThenNeutralResource(string culture, string expected)
    {
        using var scope = new CultureScope(culture);
        using ServiceProvider provider = CreateProvider();
        IStringLocalizer<IdentityResources> localizer = provider.GetRequiredService<IStringLocalizer<IdentityResources>>();

        LocalizedString result = localizer["identity.registration.welcomeSubject"];

        result.Value.ShouldBe(expected);
        result.ResourceNotFound.ShouldBeFalse();
    }

    [Fact]
    public void Lookup_Should_ReturnMissingKeyAndFormatUsingCurrentCulture()
    {
        using var scope = new CultureScope("en", "id-ID");
        using ServiceProvider provider = CreateProvider();
        IStringLocalizer<IdentityResources> localizer = provider.GetRequiredService<IStringLocalizer<IdentityResources>>();

        LocalizedString result = localizer["Missing {0:N2}", 1234.5m];

        result.Value.ShouldBe("Missing 1.234,50");
        result.ResourceNotFound.ShouldBeTrue();
    }

    [Fact]
    public void FeatureResources_Should_HaveMatchingKeysInEverySupportedLocaleWithoutFallback()
    {
        using ServiceProvider provider = CreateProvider();
        RequestLocalizationOptions options = provider.GetRequiredService<IOptions<RequestLocalizationOptions>>().Value;
        var manager = new ResourceManager(typeof(IdentityResources));
        try
        {
            ResourceSet neutral = manager.GetResourceSet(CultureInfo.InvariantCulture, true, false)!;
            neutral.ShouldNotBeNull();
            string[] defaultKeys = neutral.Cast<DictionaryEntry>().Select(entry => (string)entry.Key).Order().ToArray();
            defaultKeys.ShouldNotBeEmpty();

            foreach (CultureInfo culture in options.SupportedUICultures!
                         .Where(culture => culture.Name != options.DefaultRequestCulture.UICulture.Name))
            {
                ResourceSet translated = manager.GetResourceSet(culture, true, false)!;
                translated.ShouldNotBeNull();
                translated.Cast<DictionaryEntry>().Select(entry => (string)entry.Key).Order().ShouldBe(defaultKeys);
            }
        }
        finally
        {
            manager.ReleaseAllResources();
        }
    }

    [Fact]
    public void GetAllStrings_Should_IncludeNeutralResourcesWhenParentsAreIncluded()
    {
        using var scope = new CultureScope("fr-FR");
        using ServiceProvider provider = CreateProvider();
        IStringLocalizer<IdentityResources> localizer = provider.GetRequiredService<IStringLocalizer<IdentityResources>>();

        localizer.GetAllStrings(true).ShouldContain(value =>
            value.Name == "identity.registration.welcomeSubject" && value.Value == "Welcome!");
    }

    [Fact]
    public void SharedLocalizer_Should_ReadCultureAtLookupTimeUnderConcurrentAccess()
    {
        using ServiceProvider provider = CreateProvider();
        IStringLocalizer<IdentityResources> localizer = provider.GetRequiredService<IStringLocalizer<IdentityResources>>();

        Parallel.For(0, 50, index =>
        {
            using var scope = new CultureScope(index % 2 == 0 ? "en" : "id");
            localizer["identity.registration.welcomeSubject"].Value
                .ShouldBe(index % 2 == 0 ? "Welcome!" : "Selamat datang!");
        });
    }

    [Fact]
    public void Registration_Should_UseBuiltInFactoryAndOnlyAcceptLanguage()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddServerLocalization(options => options.DefaultRequestCulture = new RequestCulture("id"));
        using ServiceProvider provider = services.BuildServiceProvider();

        RequestLocalizationOptions options = provider.GetRequiredService<IOptions<RequestLocalizationOptions>>().Value;

        options.DefaultRequestCulture.UICulture.Name.ShouldBe("id");
        options.RequestCultureProviders.ShouldHaveSingleItem()
            .ShouldBeOfType<AcceptLanguageHeaderRequestCultureProvider>();
        provider.GetRequiredService<IStringLocalizerFactory>().ShouldBeOfType<ResourceManagerStringLocalizerFactory>();
    }

    private static ServiceProvider CreateProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddServerLocalization();
        return services.BuildServiceProvider();
    }
}
