using Microsoft.AspNetCore.OpenApi;

namespace AspNetVue.Server.Common.OpenApi;

internal static class OpenApiExtensions
{
    internal static IServiceCollection AddOpenApiWithAuth(this IServiceCollection services)
    {
        services.AddOpenApi("v1", options => options.AddDocumentTransformer<BearerSecuritySchemeTransformer>());

        return services;
    }
}
