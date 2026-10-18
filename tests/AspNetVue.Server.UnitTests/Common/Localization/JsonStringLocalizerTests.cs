using System.Globalization;
using System.Reflection;
using System.Text;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using AspNetVue.Server.Common.Localization;

namespace AspNetVue.Server.UnitTests.Common.Localization;

public sealed class JsonStringLocalizerTests
{
    [Theory]
    [InlineData("id-ID", "regional", "Regional")]
    [InlineData("id-ID", "greeting", "Halo")]
    [InlineData("id-ID", "defaultOnly", "Default")]
    [InlineData("fr-FR", "greeting", "Hello")]
    [InlineData("", "greeting", "Hello")]
    public void Lookup_Should_FollowCultureParentsThenDefault(string culture, string key, string expected)
    {
        using var scope = new CultureScope(culture);
        JsonStringLocalizer localizer = CreateLocalizer(CreateAssembly());

        LocalizedString result = localizer[key];

        result.Value.ShouldBe(expected);
        result.ResourceNotFound.ShouldBeFalse();
    }

    [Fact]
    public void Lookup_Should_ReturnMissingKeyAndLogWarning()
    {
        using var scope = new CultureScope("id");
        ILogger<JsonStringLocalizer> logger = Substitute.For<ILogger<JsonStringLocalizer>>();
        CountingResourceAssembly assembly = CreateAssembly();
        var localizer = new JsonStringLocalizer(
            assembly, "First", assembly.GetManifestResourceNames(), CultureInfo.GetCultureInfo("en"), logger);

        LocalizedString result = localizer["missing"];

        result.Value.ShouldBe("missing");
        result.ResourceNotFound.ShouldBeTrue();
        result.SearchedLocation.ShouldBe("First");
        logger.ReceivedCalls().ShouldContain(call => call.GetMethodInfo().Name == "Log");
    }

    [Fact]
    public void Lookup_Should_FormatUsingCurrentCultureRatherThanUICulture()
    {
        using var scope = new CultureScope("en", "id-ID");
        JsonStringLocalizer localizer = CreateLocalizer(CreateAssembly());

        LocalizedString result = localizer["amount", 1234.5m];

        result.Value.ShouldBe("Amount: 1.234,50");
        result.ResourceNotFound.ShouldBeFalse();
    }

    [Fact]
    public void Lookup_Should_FormatMissingKeyAndPreserveMissingFlag()
    {
        JsonStringLocalizer localizer = CreateLocalizer(CreateAssembly());

        LocalizedString result = localizer["Missing {0}", "value"];

        result.Value.ShouldBe("Missing value");
        result.ResourceNotFound.ShouldBeTrue();
    }

    [Fact]
    public void GetAllStrings_Should_IncludeOnlyCurrentCulture_WhenParentsAreExcluded()
    {
        using var scope = new CultureScope("id-ID");
        JsonStringLocalizer localizer = CreateLocalizer(CreateAssembly());

        localizer.GetAllStrings(false).Select(value => value.Name).ShouldBe(["regional"]);
    }

    [Fact]
    public void GetAllStrings_Should_MergeParentsAndDefaultWithoutOverridingCloserCulture()
    {
        using var scope = new CultureScope("id-ID");
        JsonStringLocalizer localizer = CreateLocalizer(CreateAssembly());

        var strings = localizer.GetAllStrings(true)
            .ToDictionary(value => value.Name, value => value.Value);

        strings.Count.ShouldBe(4);
        strings["regional"].ShouldBe("Regional");
        strings["greeting"].ShouldBe("Halo");
        strings["defaultOnly"].ShouldBe("Default");
    }

    [Fact]
    public void Lookup_Should_ReadEachCatalogOnceUnderConcurrentAccess()
    {
        CountingResourceAssembly assembly = CreateAssembly();
        JsonStringLocalizer localizer = CreateLocalizer(assembly);

        Parallel.For(0, 50, _ =>
        {
            using var scope = new CultureScope("id-ID");
            localizer["greeting"].Value.ShouldBe("Halo");
            localizer["defaultOnly"].Value.ShouldBe("Default");
        });

        assembly.ReadCount.ShouldBe(3);
    }

    [Fact]
    public void Lookup_Should_KeepOwnersAndCulturesSeparate()
    {
        CountingResourceAssembly assembly = CreateAssembly();
        JsonStringLocalizer first = CreateLocalizer(assembly);
        var second = new JsonStringLocalizer(assembly, "Second", assembly.GetManifestResourceNames(),
            CultureInfo.GetCultureInfo("en"), NullLogger<JsonStringLocalizer>.Instance);

        using (new CultureScope("en"))
        {
            first["greeting"].Value.ShouldBe("Hello");
            second["greeting"].Value.ShouldBe("Other owner");
        }

        using (new CultureScope("id"))
        {
            first["greeting"].Value.ShouldBe("Halo");
            second["greeting"].Value.ShouldBe("Other owner");
        }
    }

    [Theory]
    [InlineData("[]")]
    [InlineData("{\"nested\":{\"value\":\"bad\"}}")]
    [InlineData("{\"nullValue\":null}")]
    [InlineData("{\"number\":1}")]
    [InlineData("{\"\":\"empty key\"}")]
    [InlineData("{\"same\":\"first\",\"same\":\"second\"}")]
    public void Lookup_Should_RejectInvalidCatalogShape(string json)
    {
        using var scope = new CultureScope("en");
        var assembly = new CountingResourceAssembly(new Dictionary<string, string>
        {
            ["First.Resources.en.json"] = json
        });
        JsonStringLocalizer localizer = CreateLocalizer(assembly);

        Should.Throw<InvalidDataException>(() => localizer["key"]);
    }

    private static JsonStringLocalizer CreateLocalizer(CountingResourceAssembly assembly)
    {
        return new JsonStringLocalizer(assembly, "First", assembly.GetManifestResourceNames(),
            CultureInfo.GetCultureInfo("en"), NullLogger<JsonStringLocalizer>.Instance);
    }

    private static CountingResourceAssembly CreateAssembly()
    {
        return new CountingResourceAssembly(new Dictionary<string, string>
        {
            ["First.Resources.en.json"] = """
                {"greeting":"Hello","defaultOnly":"Default","amount":"Amount: {0:N2}"}
                """,
            ["First.Resources.id.json"] = """{"greeting":"Halo"}""",
            ["First.Resources.id-ID.json"] = """{"regional":"Regional"}""",
            ["Second.Resources.en.json"] = """{"greeting":"Other owner"}"""
        });
    }

    private sealed class CountingResourceAssembly(Dictionary<string, string> resources) : Assembly
    {
        private int _readCount;

        public int ReadCount => _readCount;

        public override string[] GetManifestResourceNames() => resources.Keys.ToArray();

        public override Stream? GetManifestResourceStream(string name)
        {
            Interlocked.Increment(ref _readCount);
            return resources.TryGetValue(name, out string? json)
                ? new MemoryStream(Encoding.UTF8.GetBytes(json))
                : null;
        }
    }
}
