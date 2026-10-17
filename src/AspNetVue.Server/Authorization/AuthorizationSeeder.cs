using Microsoft.EntityFrameworkCore;
using AspNetVue.Server.Database;
using AspNetVue.Server.Database.Entities.Identity;

namespace AspNetVue.Server.Authorization;

/// <summary>
/// Idempotently reconciles the database with the permission catalog discovered from the features:
/// every catalog permission exists, the <see cref="RoleNames.Admin"/> role holds all of them, and
/// each role listed in <see cref="PermissionDefinition.DefaultRoles"/> holds its own.
/// </summary>
internal sealed class AuthorizationSeeder(
    ApplicationDbContext db,
    IEnumerable<IPermissionCatalog> permissionCatalogs,
    ILogger<AuthorizationSeeder> logger)
{
    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        Dictionary<string, HashSet<string>> defaultRolesByPermission = CollectCatalog();

        await UpsertPermissionsAsync(defaultRolesByPermission.Keys, cancellationToken);

        Dictionary<string, Guid> permissionIds = await db.Permissions
            .ToDictionaryAsync(permission => permission.Name, permission => permission.Id, StringComparer.Ordinal, cancellationToken);

        await UpsertRolesAsync(defaultRolesByPermission, permissionIds, cancellationToken);

        logger.LogInformation("Authorization seed ensured {PermissionCount} permissions.", permissionIds.Count);
    }

    private Dictionary<string, HashSet<string>> CollectCatalog()
    {
        Dictionary<string, HashSet<string>> defaultRolesByPermission = new(StringComparer.Ordinal);

        foreach (PermissionDefinition definition in permissionCatalogs.SelectMany(catalog => catalog.Permissions))
        {
            if (!defaultRolesByPermission.TryGetValue(definition.Name, out HashSet<string>? roles))
            {
                roles = new HashSet<string>(StringComparer.Ordinal);
                defaultRolesByPermission[definition.Name] = roles;
            }

            roles.UnionWith(definition.DefaultRoles);
        }

        return defaultRolesByPermission;
    }

    private async Task UpsertPermissionsAsync(IEnumerable<string> permissionNames, CancellationToken cancellationToken)
    {
        HashSet<string> existingNames = await db.Permissions
            .Select(permission => permission.Name)
            .ToHashSetAsync(cancellationToken);

        string[] missingNames = [.. permissionNames.Except(existingNames, StringComparer.Ordinal)];

        if (missingNames.Length == 0)
        {
            return;
        }

        db.Permissions.AddRange(missingNames.Select(name => new Permission { Id = Guid.NewGuid(), Name = name }));

        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task UpsertRolesAsync(
        Dictionary<string, HashSet<string>> defaultRolesByPermission,
        Dictionary<string, Guid> permissionIds,
        CancellationToken cancellationToken)
    {
        HashSet<string> desiredRoleNames = new(StringComparer.Ordinal) { RoleNames.Admin };
        desiredRoleNames.UnionWith(defaultRolesByPermission.Values.SelectMany(roles => roles));

        List<Role> roles = await db.Roles
            .Include(role => role.RolePermissions)
            .ToListAsync(cancellationToken);

        var rolesByName = roles.ToDictionary(role => role.Name, StringComparer.Ordinal);

        foreach (string roleName in desiredRoleNames)
        {
            if (!rolesByName.TryGetValue(roleName, out Role? role))
            {
                role = new Role { Id = Guid.NewGuid(), Name = roleName };
                db.Roles.Add(role);
                rolesByName[roleName] = role;
            }

            foreach (string permissionName in PermissionsFor(roleName, defaultRolesByPermission))
            {
                Guid permissionId = permissionIds[permissionName];

                if (role.RolePermissions.Any(rolePermission => rolePermission.PermissionId == permissionId))
                {
                    continue;
                }

                role.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = permissionId });
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static IEnumerable<string> PermissionsFor(
        string roleName,
        Dictionary<string, HashSet<string>> defaultRolesByPermission) =>
        roleName == RoleNames.Admin
            ? defaultRolesByPermission.Keys
            : defaultRolesByPermission
                .Where(pair => pair.Value.Contains(roleName))
                .Select(pair => pair.Key);
}
