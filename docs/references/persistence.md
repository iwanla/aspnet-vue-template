# Persistence

This reference describes the implemented persistence mechanisms. For ownership,
dependency rules, and transaction guarantees, see
[Application Architecture](../../ARCHITECTURE.md). Server paths below are relative
to `src/AspNetVue.Server/`.

## Context and mappings

`Database/ApplicationDbContext.cs` owns EF persistence orchestration and model
discovery. PostgreSQL is the durable source of truth; migrations describe versioned
schema transitions. See the [Feature Development Guide](feature-development.md)
for entity/mapping examples and the migration checklist.

The context exposes named DbSets only for shared identity/access entities. Handlers
use `context.Set<T>()` for feature entities so `Database` does not reference
`Features`. `ApplyConfigurationsFromAssembly` discovers both shared and
feature-owned mappings. Feature mappings must explicitly preserve intended table
names rather than relying on a feature DbSet property to provide them.

## Auditing and soft delete

`Common/Domain/BaseEntity` is the base for persisted, audited entities. It adds
`Id`, create/update columns, actor id/name columns, and `DeletedAt`.
`ApplicationDbContext` stamps them from `IDateTimeProvider` and `IUserContext`
before saving. Handlers do not set `CreatedAt`/`UpdatedAt` and must not add their
own audit columns. The actor is null for work without an authenticated request,
such as seeding or registration.

Deletes are soft: a `Removed` entity is rewritten to `Modified` with `DeletedAt`
set, and a global query filter (`DeletedAt IS NULL`) hides it from normal queries.

- Reserve `IgnoreQueryFilters()` for explicit audits/administration, never for
  business reads.
- Unique indexes on audited tables are partial (`WHERE deleted_at IS NULL`), so a
  value is reusable once the row that held it is soft-deleted.
- Join entities (`UserRole`, `RolePermission`) are not audited and still hard-delete.
  Matching query filters hide `UserRole` when its user or role is soft-deleted, and
  `RolePermission` when its role or permission is soft-deleted. Persisted join rows
  may remain, but direct queries and required-navigation queries agree on visibility.

## Domain-event dispatch

Entities collect events through `Entity.Raise`. `ApplicationDbContext` extracts
and clears pending events before saving:

```text
Entity raises event -> DbContext extracts/clears pending events
                    -> EF SaveChangesAsync succeeds
                    -> Dispatcher resolves handlers in new scopes and awaits them
```

`Database/DomainEventsDispatcher.cs` dispatches in-process after save, not through
a transactional outbox or durable queue. Event handlers do not share the originating
context's unit of work. If dispatch fails after the database save succeeds,
persisted data is not automatically rolled back, and delivery/retry is not
guaranteed. Do not rely on this mechanism for atomic external side effects.

## Verification

Use real PostgreSQL integration tests for mappings, uniqueness, foreign keys,
migrations, audit/soft-delete behavior, and join visibility. Isolated handler tests
can verify orchestration but InMemory tests do not establish relational behavior.
Test event reactions and post-save failures without assuming rollback or retries.
