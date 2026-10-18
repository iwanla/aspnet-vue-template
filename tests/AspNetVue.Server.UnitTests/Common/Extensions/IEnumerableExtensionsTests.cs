using AspNetVue.Server.Common.Extensions;

namespace AspNetVue.Server.UnitTests.Common.Extensions;

public sealed class IEnumerableExtensionsTests
{
    [Fact]
    public void IsNullOrEmpty_Should_ReturnTrue_ForNullSequence()
    {
        IEnumerable<int>? source = null;

        source.IsNullOrEmpty().ShouldBeTrue();
    }

    [Fact]
    public void IsNullOrEmpty_Should_ReturnTrue_ForEmptySequence()
    {
        IEnumerable<int> source = [];

        source.IsNullOrEmpty().ShouldBeTrue();
    }

    [Fact]
    public void IsNullOrEmpty_Should_StopAfterFirstElement()
    {
        FirstElementOnly().IsNullOrEmpty().ShouldBeFalse();

        static IEnumerable<int> FirstElementOnly()
        {
            yield return 1;
            throw new InvalidOperationException("The second element must not be requested.");
        }
    }

    [Fact]
    public void WhereNotNull_Should_PreserveOrderAndDuplicates()
    {
        string?[] source = [null, "first", null, "second", "first", null];

        source.WhereNotNull().ShouldBe(["first", "second", "first"]);
    }

    [Fact]
    public void WhereNotNull_Should_ReturnEmpty_ForEmptyOrAllNullSequence()
    {
        string?[] empty = [];
        string?[] allNull = [null, null];

        empty.WhereNotNull().ShouldBeEmpty();
        allNull.WhereNotNull().ShouldBeEmpty();
    }

    [Fact]
    public void WhereNotNull_Should_RejectNullSourceImmediately()
    {
        IEnumerable<string?> source = null!;

        Should.Throw<ArgumentNullException>(() => source.WhereNotNull())
            .ParamName.ShouldBe("source");
    }

    [Fact]
    public void WhereNotNull_Should_DeferEnumeration()
    {
        bool enumerated = false;

        IEnumerable<string> result = Source().WhereNotNull();

        enumerated.ShouldBeFalse();
        result.ShouldBe(["value"]);
        enumerated.ShouldBeTrue();

        IEnumerable<string?> Source()
        {
            enumerated = true;
            yield return null;
            yield return "value";
        }
    }
}
