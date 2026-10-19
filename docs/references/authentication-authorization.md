# Authentication and Authorization

For security ownership and dependency boundaries, see
[Application Architecture](../../ARCHITECTURE.md). Server paths below are relative
to `src/AspNetVue.Server/`. For permission constants, catalog examples, and endpoint
metadata, see the [Feature Development Guide](feature-development.md).

## Identity and enforcement

`Authentication/` owns token/password mechanisms behind shared interfaces.
Credential workflows remain application use cases. Application code obtains the
caller through `IUserContext`, not independent HTTP-claim parsing. JWTs carry
identity, not permission grants.

`Authorization/` resolves endpoint capabilities:

```text
Endpoint permission metadata -> Dynamic policy -> Authorization handler
                            -> Permission provider -> Cache / database
```

Features own permission constants and implement `IPermissionCatalog` using shared
`PermissionDefinition` contracts. Assembly discovery keeps `Authorization`
independent of concrete features. Roles group database-backed grants; endpoints
check permissions, not role names. Record ownership and business preconditions
still belong in application handlers after a permission passes.

## Permission cache

Permission resolution is memoized per request and cached across requests.
Membership/grant mutations must invalidate affected `IPermissionProvider` entries;
expiration is a fallback freshness bound, not a substitute for invalidation.

Permission lookup failures deny access. The current provider uses HybridCache
directly; it has no separate explicit cache-failure-to-database fallback path.
Client visibility checks do not replace server enforcement.

## Catalog seeding and initialization

Startup seeding adds missing catalog permissions and grants. Removing a catalog
entry does not by itself revoke persisted grants or delete permissions. Removing
a default role also does not revoke existing grants. The seeder grants catalog
permissions to the admin role; ordinary default-role grants must be deliberate.

The startup initializer applies migrations only in Development and attempts
seeding on startup. It logs and catches initialization failures, so a running
host is not proof of a successfully initialized database. Production migrations
and configuration remain deployment responsibilities; see `README.md` and the
server `AGENTS.md` for operational guidance.

## Verification

Verify anonymous access, missing permissions, granted permissions, and record
ownership separately through HTTP integration tests. Cover permission-cache
invalidation after membership/grant changes and denial on lookup failure. Test
additive seeding without treating catalog removal as grant revocation.
