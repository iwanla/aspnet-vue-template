using System.Diagnostics.CodeAnalysis;

namespace AspNetVue.Server.Common.Extensions;

public static class IEnumerableExtensions
{
    /// <summary>
    /// Returns true for a null or empty sequence. May enumerate the first element;
    /// do not use this to probe a single-use sequence before consuming it again.
    /// </summary>
    public static bool IsNullOrEmpty<T>([NotNullWhen(false)] this IEnumerable<T>? source)
    {
        return source is null || !source.Any();
    }

    /// <summary>Filters null references lazily, preserving the order of non-null elements.</summary>
    /// <exception cref="ArgumentNullException">The source is null.</exception>
    public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> source)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(source);

        return source.OfType<T>();
    }
}
