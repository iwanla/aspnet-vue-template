namespace AspNetVue.Server.Authorization;

/// <summary>
/// Implemented by each feature that owns permissions (for example <c>TodosPermissionCatalog</c>) so
/// the catalog can be collected by assembly scan. <c>Authorization/</c> never references
/// <c>Features/</c> directly; the dependency points feature → authorization.
/// </summary>
internal interface IPermissionCatalog
{
    IReadOnlyCollection<PermissionDefinition> Permissions { get; }
}
