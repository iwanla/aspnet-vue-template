using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Localization;

namespace AspNetVue.Server.Common.Localization;

/// <summary>
/// A culture-independent localizer with immutable, lazily loaded resource catalogs.
/// Culture is read at lookup time; embedded resources require a rebuild to change.
/// </summary>
public sealed class JsonStringLocalizer : IStringLocalizer
{
    private readonly Assembly _assembly;
    private readonly string _resourceNamespace;
    private readonly HashSet<string> _resourceNames;
    private readonly CultureInfo _defaultCulture;
    private readonly ILogger<JsonStringLocalizer> _logger;
    private readonly ConcurrentDictionary<string, Lazy<FrozenDictionary<string, string>>> _catalogs = new(StringComparer.Ordinal);

    internal JsonStringLocalizer(
        Assembly assembly,
        string resourceNamespace,
        IEnumerable<string> resourceNames,
        CultureInfo defaultCulture,
        ILogger<JsonStringLocalizer> logger)
    {
        _assembly = assembly;
        _resourceNamespace = resourceNamespace;
        _resourceNames = resourceNames.ToHashSet(StringComparer.Ordinal);
        _defaultCulture = defaultCulture;
        _logger = logger;
    }

    public LocalizedString this[string name]
    {
        get
        {
            ArgumentNullException.ThrowIfNull(name);
            CultureInfo requested = CultureInfo.CurrentUICulture;
            foreach (CultureInfo culture in GetFallbackCultures(requested))
            {
                if (GetCatalog(culture).TryGetValue(name, out string? value))
                {
                    if (culture.Name == _defaultCulture.Name && culture.Name != requested.Name)
                    {
                        _logger.LogWarning(
                            "Localization key {Key} for {ResourceNamespace} in {Culture} fell back to {DefaultCulture}",
                            name, _resourceNamespace, requested.Name, _defaultCulture.Name);
                    }

                    return new LocalizedString(name, value, resourceNotFound: false, _resourceNamespace);
                }
            }

            _logger.LogWarning("Localization key {Key} for {ResourceNamespace} was not found for {Culture}",
                name, _resourceNamespace, requested.Name);
            return new LocalizedString(name, name, resourceNotFound: true, _resourceNamespace);
        }
    }

    public LocalizedString this[string name, params object[] arguments]
    {
        get
        {
            ArgumentNullException.ThrowIfNull(arguments);
            LocalizedString localized = this[name];
            string value = string.Format(CultureInfo.CurrentCulture, localized.Value, arguments);

            return new LocalizedString(name, value, localized.ResourceNotFound, localized.SearchedLocation);
        }
    }

    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
    {
        CultureInfo requested = CultureInfo.CurrentUICulture;
        IEnumerable<CultureInfo> cultures = includeParentCultures ? GetFallbackCultures(requested) : [requested];
        var strings = new Dictionary<string, LocalizedString>(StringComparer.Ordinal);

        foreach (CultureInfo culture in cultures)
        {
            foreach ((string key, string value) in GetCatalog(culture))
            {
                strings.TryAdd(key, new LocalizedString(key, value, resourceNotFound: false, _resourceNamespace));
            }
        }

        return strings.Values.ToArray();
    }

    private IEnumerable<CultureInfo> GetFallbackCultures(CultureInfo requested)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (CultureInfo root in new[] { requested, _defaultCulture })
        {
            for (CultureInfo culture = root; culture != CultureInfo.InvariantCulture; culture = culture.Parent)
            {
                if (seen.Add(culture.Name))
                {
                    yield return culture;
                }
            }
        }
    }

    private FrozenDictionary<string, string> GetCatalog(CultureInfo culture)
    {
        string resourceName = $"{_resourceNamespace}.Resources.{culture.Name}.json";
        if (!_resourceNames.Contains(resourceName))
        {
            return FrozenDictionary<string, string>.Empty;
        }

        return _catalogs.GetOrAdd(culture.Name, _ => new Lazy<FrozenDictionary<string, string>>(
            () => LoadCatalog(resourceName))).Value;
    }

    private FrozenDictionary<string, string> LoadCatalog(string resourceName)
    {
        using Stream stream = _assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidDataException($"Embedded localization resource '{resourceName}' is missing.");
        using var document = JsonDocument.Parse(stream);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidDataException($"Localization resource '{resourceName}' must be a JSON object.");
        }

        var catalog = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (JsonProperty property in document.RootElement.EnumerateObject())
        {
            if (string.IsNullOrWhiteSpace(property.Name) || property.Value.ValueKind != JsonValueKind.String ||
                !catalog.TryAdd(property.Name, property.Value.GetString()!))
            {
                throw new InvalidDataException(
                    $"Localization resource '{resourceName}' requires unique, non-empty keys and string values.");
            }
        }

        return catalog.ToFrozenDictionary(StringComparer.Ordinal);
    }
}
