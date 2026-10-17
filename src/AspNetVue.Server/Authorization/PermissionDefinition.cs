namespace AspNetVue.Server.Authorization;

/// <summary>
/// A permission declared by a feature together with the roles that receive it by default when the
/// catalog is seeded. The <see cref="RoleNames.Admin"/> role always receives every declared
/// permission, so a feature only lists the extra roles (usually <see cref="RoleNames.User"/>).
/// </summary>
internal readonly record struct PermissionDefinition(string Name, IReadOnlyList<string> DefaultRoles);
