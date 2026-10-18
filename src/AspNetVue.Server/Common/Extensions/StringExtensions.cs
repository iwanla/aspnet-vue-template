using System.Diagnostics.CodeAnalysis;

namespace AspNetVue.Server.Common.Extensions;

public static class StringExtensions
{
    /// <summary>Trims surrounding whitespace, returning null when nothing remains.</summary>
    public static string? TrimToNull(this string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    /// <summary>
    /// Limits a string to the specified number of UTF-16 code units, without an ellipsis.
    /// Returns null for a null input. This is not grapheme-aware display truncation.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">The maximum length is negative.</exception>
    [return: NotNullIfNotNull(nameof(value))]
    public static string? Truncate(this string? value, int maxLength)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(maxLength);

        return value is null || value.Length <= maxLength ? value : value[..maxLength];
    }
}
