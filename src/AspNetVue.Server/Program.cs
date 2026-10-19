using System.Reflection;
using Asp.Versioning;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Serilog;
using AspNetVue.Server;
using AspNetVue.Server.Common.Endpoints;
using AspNetVue.Server.Common.Localization;
using AspNetVue.Server.Common.RateLimiting;
using AspNetVue.Server.Common.OpenApi;
using AspNetVue.Server.Common.Telemetry;
using AspNetVue.Server.Hosting;
using Asp.Versioning.Builder;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, loggerConfig) => loggerConfig.ReadFrom.Configuration(context.Configuration));

builder.Services.AddOpenApiWithAuth();
builder.Services.AddServerLocalization();

builder.Services
    .AddApplication()
    .AddPresentation()
    .AddInfrastructure(builder.Configuration);

builder.Services.AddObservability(builder.Configuration, builder.Environment.ApplicationName);

builder.Services.AddRateLimitingInternal(builder.Configuration);

builder.Services.AddCorsInternal(builder.Configuration);

builder.Services.AddEndpoints(Assembly.GetExecutingAssembly());

WebApplication app = builder.Build();

ApiVersionSet versionSet = app.NewApiVersionSet()
    .HasApiVersion(new ApiVersion(1, 0))
    .ReportApiVersions()
    .Build();

app.MapEndpoints(app.MapGroup("api/v{version:apiVersion}").WithApiVersionSet(versionSet));

if (app.Environment.IsDevelopment())
{
    app.MapOpenApiWithScalar();

    // app.ApplyMigrations();
}

app.MapHealthChecks("api/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.UseSecurityHeaders();

app.UseRequestContextLogging();

app.UseSerilogRequestLogging();

app.UseExceptionHandler();

app.UseRequestLocalization();

app.UseStaticFiles();

app.UseCors();

app.UseAuthentication();

app.UseAuthorization();

app.UseRateLimiter();

// REMARK: If you want to use Controllers, you'll need this.
app.MapControllers();

// SPA hosting: unknown API routes -> 404 JSON (never swallowed by index.html).
// All API routes live under the /api prefix (see MapGroup above).
app.Map("api/{**catch-all}", () => Results.NotFound(new { error = "Not Found" }));

// Fallback SPA: semua non-API yang tidak match file statis -> index.html (Vue Router history mode).
app.MapFallbackToFile("index.html");

await app.RunAsync();

// REMARK: Required for functional and integration tests to work.
namespace AspNetVue.Server
{
    public partial class Program;
}
