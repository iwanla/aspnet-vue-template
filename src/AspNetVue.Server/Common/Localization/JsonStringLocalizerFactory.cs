using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace AspNetVue.Server.Common.Localization;

/// <summary>
/// Resolves the nearest namespace owning embedded Resources/{culture}.json files.
/// Discovery uses resource metadata, never references to concrete feature types.
/// </summary>
public sealed class JsonStringLocalizerFactory(
    IOptions<RequestLocalizationOptions> options,
    ILoggerFactory loggerFactory) : IStringLocalizerFactory
{
    private readonly ConcurrentDictionary<Assembly, string[]> _resourceNames = new();
    private readonly ConcurrentDictionary<(Assembly Assembly, string ResourceNamespace), Lazy<JsonStringLocalizer>> _localizers = new();

    public IStringLocalizer Create(Type resourceSource)
    {
        ArgumentNullException.ThrowIfNull(resourceSource);

        return Create(resourceSource.Assembly, resourceSource.Namespace ?? resourceSource.Name);
    }

    public IStringLocalizer Create(string baseName, string location)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(baseName);
        ArgumentException.ThrowIfNullOrWhiteSpace(location);

        return Create(Assembly.Load(new AssemblyName(location)), baseName);
    }

    private JsonStringLocalizer Create(Assembly assembly, string baseName)
    {
        string[] names = _resourceNames.GetOrAdd(assembly, static value => value.GetManifestResourceNames());
        string resourceNamespace = FindResourceNamespace(baseName, names);

        return _localizers.GetOrAdd((assembly, resourceNamespace), key => new Lazy<JsonStringLocalizer>(() =>
            new JsonStringLocalizer(
                key.Assembly,
                key.ResourceNamespace,
                names,
                options.Value.DefaultRequestCulture.UICulture,
                loggerFactory.CreateLogger<JsonStringLocalizer>()))).Value;
    }

    private static string FindResourceNamespace(string baseName, string[] resourceNames)
    {
        string candidate = baseName;
        while (true)
        {
            string prefix = candidate + ".Resources.";
            if (resourceNames.Any(name => name.StartsWith(prefix, StringComparison.Ordinal) &&
                                          name.EndsWith(".json", StringComparison.Ordinal)))
            {
                return candidate;
            }

            int separator = candidate.LastIndexOf('.');
            if (separator < 0)
            {
                return baseName;
            }

            candidate = candidate[..separator];
        }
    }
}
