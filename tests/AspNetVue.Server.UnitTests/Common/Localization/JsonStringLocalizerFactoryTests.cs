using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using AspNetVue.Server.Common.Localization;
using AspNetVue.Server.Features.Identity;
using AspNetVue.Server.Features.Identity.Register;

namespace AspNetVue.Server.UnitTests.Common.Localization;

public sealed class JsonStringLocalizerFactoryTests
{
    [Fact]
    public void Factory_Should_ShareLocalizerAcrossOverloadsAndTypesWithSameResourceOwner()
    {
        using ServiceProvider provider = CreateProvider();
        IStringLocalizerFactory factory = provider.GetRequiredService<IStringLocalizerFactory>();

        IStringLocalizer first = factory.Create(typeof(UserRegisteredDomainEventHandler));
        IStringLocalizer second = factory.Create(typeof(IdentityErrors));
        IStringLocalizer byName = factory.Create(typeof(UserRegisteredDomainEventHandler).FullName!,
            typeof(IdentityErrors).Assembly.GetName().Name!);

        first.ShouldBeSameAs(second);
        first.ShouldBeSameAs(byName);
    }

    [Fact]
    public void Registration_Should_ResolveGenericLocalizerUsingEmbeddedFeatureResources()
    {
        using var scope = new CultureScope("id-ID");
        using ServiceProvider provider = CreateProvider();

        IStringLocalizer<UserRegisteredDomainEventHandler> localizer =
            provider.GetRequiredService<IStringLocalizer<UserRegisteredDomainEventHandler>>();

        localizer["identity.registration.welcomeSubject"].Value.ShouldBe("Selamat datang!");
    }

    [Fact]
    public void FeatureResources_Should_HaveMatchingKeysInEverySupportedLocale()
    {
        using ServiceProvider provider = CreateProvider();
        IStringLocalizerFactory factory = provider.GetRequiredService<IStringLocalizerFactory>();
        IStringLocalizer localizer = factory.Create(typeof(IdentityErrors));
        string[] defaultKeys;
        using (new CultureScope("en"))
        {
            defaultKeys = localizer.GetAllStrings(false).Select(value => value.Name).Order().ToArray();
        }

        defaultKeys.ShouldNotBeEmpty();
        using (new CultureScope("id"))
        {
            localizer.GetAllStrings(false).Select(value => value.Name).Order().ShouldBe(defaultKeys);
        }
    }

    [Fact]
    public void Factory_Should_ReturnMissingKey_WhenOwnerHasNoResources()
    {
        using ServiceProvider provider = CreateProvider();
        IStringLocalizerFactory factory = provider.GetRequiredService<IStringLocalizerFactory>();

        LocalizedString result = factory.Create(typeof(JsonStringLocalizerFactoryTests))["missing"];

        result.ResourceNotFound.ShouldBeTrue();
        result.Value.ShouldBe("missing");
    }

    [Fact]
    public void Registration_Should_UseOnlyAcceptLanguageAndAllowCustomDefaults()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddJsonLocalization(options => options.DefaultRequestCulture = new RequestCulture("id"));
        using ServiceProvider provider = services.BuildServiceProvider();

        RequestLocalizationOptions options = provider.GetRequiredService<IOptions<RequestLocalizationOptions>>().Value;

        options.DefaultRequestCulture.UICulture.Name.ShouldBe("id");
        options.RequestCultureProviders.ShouldHaveSingleItem()
            .ShouldBeOfType<AcceptLanguageHeaderRequestCultureProvider>();
        provider.GetRequiredService<IStringLocalizerFactory>().ShouldBeOfType<JsonStringLocalizerFactory>();
    }

    private static ServiceProvider CreateProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddJsonLocalization();
        return services.BuildServiceProvider();
    }
}
