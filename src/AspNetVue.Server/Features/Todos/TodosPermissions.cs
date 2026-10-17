namespace AspNetVue.Server.Features.Todos;

/// <summary>
/// Permissions owned by the Todos feature. Names are constants here (never string literals at the
/// call site) and <see cref="TodosPermissionCatalog"/> exposes them to <c>Authorization/</c>.
/// </summary>
internal static class TodosPermissions
{
    internal const string TodosRead = "todos:read";

    internal const string TodosCreate = "todos:create";

    internal const string TodosUpdate = "todos:update";

    internal const string TodosDelete = "todos:delete";
}
