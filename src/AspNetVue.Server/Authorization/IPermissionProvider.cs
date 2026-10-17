namespace AspNetVue.Server.Authorization;

/// <summary>
/// Resolves the permission set of a user. The database is the source of truth (never the JWT) and
/// the result is cached for a short TTL. An empty set is the safe answer: callers must deny access
/// when permissions cannot be resolved.
/// </summary>
internal interface IPermissionProvider
{
    Task<IReadOnlySet<string>> GetForUserIdAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Drops the cached permission set of a user. Call it whenever the roles or permissions of that
    /// user change; the TTL is only a safety net.
    /// </summary>
    Task InvalidateAsync(Guid userId, CancellationToken cancellationToken);
}
