using AspNetVue.Server.Common.Extensions;

namespace AspNetVue.Server.UnitTests.Common.Extensions;

public sealed class StringExtensionsTests
{
    [Theory]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData(" \t\r\n", null)]
    [InlineData("\u2003", null)]
    [InlineData("  hello world  ", "hello world")]
    [InlineData("hello", "hello")]
    public void TrimToNull_Should_NormalizeWhitespace(string? value, string? expected)
    {
        value.TrimToNull().ShouldBe(expected);
    }

    [Theory]
    [InlineData(null, 3, null)]
    [InlineData("", 0, "")]
    [InlineData("hello", 0, "")]
    [InlineData("hello", 3, "hel")]
    [InlineData("hello", 5, "hello")]
    [InlineData("hello", 10, "hello")]
    public void Truncate_Should_RespectMaximumLength(string? value, int maxLength, string? expected)
    {
        value.Truncate(maxLength).ShouldBe(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("hello")]
    public void Truncate_Should_RejectNegativeMaximumLength(string? value)
    {
        Should.Throw<ArgumentOutOfRangeException>(() => value.Truncate(-1))
            .ParamName.ShouldBe("maxLength");
    }
}
