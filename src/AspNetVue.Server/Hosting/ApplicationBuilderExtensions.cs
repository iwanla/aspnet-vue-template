using Scalar.AspNetCore;

namespace AspNetVue.Server.Hosting;

public static class ApplicationBuilderExtensions
{
    public static WebApplication MapOpenApiWithScalar(this WebApplication app)
    {
        app.MapOpenApi();
        app.MapScalarApiReference();

        return app;
    }
}
