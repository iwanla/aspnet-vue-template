using AspNetVue.Server.Authorization;

namespace AspNetVue.Server.Features.Todos;

/// <summary>Exposes the Todos permissions to the assembly scan in <c>Authorization/</c>.</summary>
internal sealed class TodosPermissionCatalog : IPermissionCatalog
{
    public IReadOnlyCollection<PermissionDefinition> Permissions { get; } =
    [
        new(TodosPermissions.TodosRead, [RoleNames.User]),
        new(TodosPermissions.TodosCreate, [RoleNames.User]),
        new(TodosPermissions.TodosUpdate, [RoleNames.User]),
        new(TodosPermissions.TodosDelete, [RoleNames.User])
    ];
}
