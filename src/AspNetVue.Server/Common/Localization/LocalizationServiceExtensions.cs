using Microsoft.AspNetCore.Localization;

namespace AspNetVue.Server.Common.Localization;

public static class LocalizationServiceExtensions
{
    /// <summary>Registers RESX localization and Accept-Language culture selection.</summary>
    public static IServiceCollection AddServerLocalization(
        this IServiceCollection services,
        Action<RequestLocalizationOptions>? configure = null)
    {
        services.AddLocalization();
        services.Configure<RequestLocalizationOptions>(options =>
        {
            options.DefaultRequestCulture = new RequestCulture("en");
            options.AddSupportedCultures("en", "id");
            options.AddSupportedUICultures("en", "id");
            options.FallBackToParentCultures = true;
            options.FallBackToParentUICultures = true;
            options.RequestCultureProviders = [new AcceptLanguageHeaderRequestCultureProvider()];
            configure?.Invoke(options);
        });

        return services;
    }
}
