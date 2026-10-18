using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Localization;

namespace AspNetVue.Server.Common.Localization;

public static class JsonLocalizationServiceExtensions
{
    /// <summary>Registers embedded JSON resources and Accept-Language culture selection.</summary>
    public static IServiceCollection AddJsonLocalization(
        this IServiceCollection services,
        Action<RequestLocalizationOptions>? configure = null)
    {
        services.AddLocalization();
        services.Replace(ServiceDescriptor.Singleton<IStringLocalizerFactory, JsonStringLocalizerFactory>());
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
