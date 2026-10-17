# ARCHITECTURE

The server is a 1:1 port of the vertical-slice template (`Web.Api` → `AspNetVue.Server`)
with minimal adaptations for SPA hosting, file conventions, `Integrations/`,
and template distribution. The explicit list of deviations is in §8.

## 1. Vertical slices (`Features/*`)

One public type per file, prefixed with the use-case name:

```text
Features/{Domain}/{UseCase}/{UseCase}{Command,Validator,Handler,Endpoint,Request,Query,Response}.cs
```

Namespaces follow the folder structure: `AspNetVue.Server.Features.{Domain}.{UseCase}`.
Use-case types retain their prefixes (for example, `CreateTodoCommand`) for explicit
references and easy Go-to-Type navigation. Shared domain types stay in
`AspNetVue.Server.Features.{Domain}`. Renaming a use-case means renaming its folder,
namespace, and prefixed types.

No MediatR: `ICommand/IQuery` are scanned by Scrutor (scoped) in
`DependencyInjection.AddApplication()`, decorated with
`ValidationDecorator` (outer) → `LoggingDecorator` (inner).
Endpoint auto-discovery via `AddEndpoints(Assembly)` + `MapEndpoints()`;
shape: `Command/Query record` + FluentValidation `Validator` +
`internal sealed Handler` + `sealed Endpoint : IEndpoint`.
`Result/Result<T> + Error(code, description, ErrorType)` → `CustomResults.Problem`
(RFC ProblemDetails: 400/404/409/500).

## 2. Auth / Authz / Data

- JWT Bearer (`Jwt:Secret/Issuer/Audience/ExpirationInMinutes`), PBKDF2-SHA512,
  7-day refresh-token rotation. `IUserContext` comes from `NameIdentifier`.
- Permission-based: `HasPermissionAttribute` / `.HasPermission(permission)` → dynamic policy
  (`PermissionAuthorizationPolicyProvider`) → `PermissionAuthorizationHandler` →
  `IPermissionProvider` (database + 2-minute cache). Roles/permissions are seeded at startup
  by `AuthorizationDataInitializer` and enforced at the endpoint only. Full rules: §10.
- Identity/access entities (`User`, `RefreshToken`, `Role`, `Permission`, `UserRole`,
  `RolePermission`) live in `Database/Entities/Identity/`
  (`AspNetVue.Server.Database.Entities.Identity`) because both `Authentication/` and
  `Authorization/` consume them; feature entities stay in their feature root (§11).
- EF Core + PostgreSQL 17, snake_case, `public` schema, domain-event dispatch
  post-commit (`Entity` → `DomainEventsDispatcher` via Scrutor `IDomainEventHandler<>`).
  4 bundled migrations; `ApplyMigrations()` stays commented (the `AuthorizationDataInitializer`
  hosted service migrates in Development and always seeds).

## 3. Cross-cutting

- **Cache** (`Integrations/Cache/`): `HybridCache` is the only abstraction,
  configured via `AddCacheIntegration(configuration)`. Default is L1 in-memory
  (`Cache:Provider=Memory`, `DefaultExpirationMinutes=5`). Set
  `Cache:Provider=Redis` + `Cache:Redis:ConnectionString` for a Redis L2
  (`AddStackExchangeRedisCache`, `InstanceName = Cache:KeyPrefix`), failing fast
  on an empty connection string. No custom `ICacheService`.
  Keys are composed per feature (e.g. `Features/Todos/TodoCacheKeys.cs`).
- **Email** (`Integrations/Email/`): `IEmailSender` + `LoggingEmailSender`
  (moved from `Notifications/`). Sole consumer: `UserRegisteredDomainEventHandler`.
  `Payments/`/`Storage`/real providers are deferred until needed.
- **Idempotency**: `Idempotency-Key` header via `IdempotencyFilter`
  (opt-in per endpoint: `.WithIdempotency()`, e.g. create todo), duplicates → 409.
- **Rate limiting** (`Common/Extensions/RateLimitingExtensions.cs`):
  global 100/min + `authentication` partition 10/min (register/login/refresh).
- **Observability/logging**: conditional OTel via env
  (`AddObservability`), Serilog (Console + Seq), security headers,
  request-context logging, global exception handler (`AddExceptionHandler` +
  `AddProblemDetails`).
- **API docs**: dev-only OpenAPI + Scalar (`/openapi/v1.json`, `/scalar/v1`).
- **Health**: `GET /health` (Npgsql check, `UIResponseWriter`).

## 4. SPA hosting (adaptation outside the 1:1 port)

- Dev: `dotnet run :5000` → `SpaProxyLaunchManager (bun run dev)` → Vite `:5173`
  (HMR); Vite fetches to `/api` → Vite proxy → `:5000`
  (no CORS). No manual `UseSpa` (automatic via `SpaHostingStartup` +
  `spa.proxy.json` from the csproj: `SpaRoot`, `SpaProxyServerUrl`, `SpaProxyLaunchCommand`).
- Prod: `bun run build` in the client → straight into `../AspNetVue.Server/wwwroot/`
  (`vite.config.ts outDir`) → `dotnet publish` single artifact;
  `UseStaticFiles` + `MapFallbackToFile(index.html)` (Vue Router history mode).
- `wwwroot/` is a build artifact (gitignored), regenerated via the client build.
- Client route `/status` (a page; the server health endpoint is `/api/health`).
- All API routes live under the `/api` prefix (unlike upstream):
  business endpoints use `api/v1/todos*` and `api/v1/users/*`, via
  `MapEndpoints(app.MapGroup("api/v{version:apiVersion}").WithApiVersionSet(versionSet))`.
  `Asp.Versioning.Http` reads the URL segment; all 11 endpoints explicitly map to v1.
  The version segment is mandatory (unversioned business paths and unsupported versions return 404).
  Health stays version-neutral at `api/health`, outside the versioned group.
  API Explorer substitutes the version into OpenAPI paths (`/openapi/v1.json`).
  Future versions must be declared on the version set, mapped per endpoint, and
  registered as separate OpenAPI documents; no endpoint is implicitly upgraded.
- Unknown `api/{**}` → `404 JSON` before the fallback,
  never swallowed by `index.html`.

## 5. Client (`src/AspNetVue.Client/`)

Vue 3 + Vite + TS + Pinia + Vue Router (`src/{app,features,shared}/`).
`vite.config.ts`: `@` alias, dev proxy to `:5000`, `outDir` into the server wwwroot.
`bun install` stays manual after scaffolding (no built-in bun post-action).

## 6. Template distribution (`dotnet new`)

- Identity `aspnetvue` (`.template.config/template.json`):
  `sourceName: AspNetVue` (namespaces, folders, slnx, vite `outDir`) +
  derived symbol `lower_name` (compose DB name, package name, `Cache:KeyPrefix`).
- Scaffold excludes: `bin/obj/node_modules/dist/wwwroot output/tsbuildinfo`,
  generated `vite.config.js/.d.ts`, and `AspNetVue.Template.csproj` itself.
- `primaryOutputs` for the 4 csproj files + NuGet restore post-action
  (opt-out with `--skipRestore`).
- `AspNetVue.Template.csproj` (`PackageType Template`) produces
  `AspNetVue.Template.*.nupkg` for `dotnet new install`.

## 7. Configuration & operations

| Section | Keys | Default |
|---|---|---|
| `ConnectionStrings` | `Database` | empty (via user-secrets/env; dev points at the compose service) |
| `Jwt` | `Secret/Issuer/Audience/ExpirationInMinutes` | empty (dev: dev secret + issuer/audience) |
| `Cors` | `AllowedOrigins` | `[]` |
| `RateLimiting` | `Global`, `Authentication` (`PermitLimit/WindowInSeconds`) | 100/min, 10/min |
| `Cache` | `Provider/DefaultExpirationMinutes/KeyPrefix/Redis:ConnectionString` | Memory, 5, `aspnetvue`, empty |

Ports: API `:5000`, Vite `:5173`, Seq `:8081`.
Compose: `app` + `postgres:17` (DB `aspnetvue`) + `seq`; the app healthcheck uses `/api/health`.

## 8. Deviations from upstream (the only intentional ones)

1. Namespace `Web.Api*` → `AspNetVue.Server*`; compose DB `clean-architecture` → `aspnetvue`.
2. SPA hosting (§4): csproj SpaProxy props, `UseStaticFiles` + guards + fallback,
   Vite proxy regex, client route `/status`.
3. One prefixed type per file (§1) — zero behavior change.
4. `Notifications/` → `Integrations/Email/`; added `Integrations/Cache/` (§3).
5. `Testcontainers.PostgreSql` 4.12.0 → 4.16.0 (4.12 fails restore with NU1903 on SSH.NET
   + `TreatWarningsAsErrors`; upstream fails the same way).
6. Template packaging (§6 + `AspNetVue.Template.csproj`).
7. `/api` prefix on all API routes (§4; upstream has none).
   Business routes additionally require URL-segment API versioning (`/api/v1/...`).
8. NOT ported (not functionality): `.claude/skills`, `.github/workflows`,
   `docker-compose.dcproj`, .NET 8/9 Dockerfiles, upstream slnx, upstream README.
9. RBAC enforced (upstream `PermissionAuthorizationHandler` auto-succeeds any authenticated
   user): roles/permissions tables + migration, catalog seeder, fail-closed handler, and a
   database-backed `IPermissionProvider` with a short cache (§2, §10).

## 9. Tests & rules

- Unit (`UnitTests`): handler tests (real `HybridCache` via `AddHybridCache()`,
  InMemory + NSubstitute) + `CacheServiceExtensionsTests` (default registers,
  Redis without a connection string throws) + `Authorization/` tests
  (`PermissionProvider` caching/invalidation, handler fail-closed + once-per-request).
- Integration (`IntegrationTests`): `WebApplicationFactory<Program>` +
  Testcontainers `postgres:17` (needs Docker Hub).
  `ApiVersioningTests` verifies routing and OpenAPI without Docker or database access;
  `AuthorizationTests` verifies 401/403/200 through the seeded roles.
- Architecture (`ArchitectureTests`, NetArchTest): handlers + endpoints must be `sealed`;
  `Authentication`/`Authorization`/`Database`/`Common` must not depend on `Features` (§11).
- Build: CPM + `TreatWarningsAsErrors` (`Directory.Build.props` /
  `Directory.Packages.props`); `dotnet build` must report 0 warnings.
  EF Core 10 no longer writes the `// <auto-generated />` header into the migration
  `.cs` file, so `dotnet ef migrations add` needs that header added manually — without it
  IDE0161 (file-scoped namespace) fails the build (and therefore blocks the build-first
  `dotnet ef migrations remove`). The Designer/snapshot files already have it.
  EF also derives a table name from the matching `DbSet` property on `ApplicationDbContext`:
  when a feature entity has no `DbSet`, set the table explicitly in the feature configuration
  (`TodoItemConfiguration.ToTable("todo_items")`) so the physical table does not move.

## 10. Server rules: permission-based authorization (RBAC)

The database is the source of truth; the JWT only carries identity (`sub`), never permissions.

Model (entities in `Database/Entities/Identity/`, enforcement in `Authorization/`):
`Users —< UserRoles >— Roles —< RolePermissions >— Permissions`.
Roles are administrative groupings only — code always checks permissions, never role names.

1. Permission names are constants owned by the feature that declares them
   (`Features/Todos/TodosPermissions.cs`, `Features/Users/UsersPermissions.cs`), format
   `<feature>:<action>` (e.g. `todos:create`). Never inline the string at the call site.
2. `Authorization/` must not reference `Features/`. Each feature exposes an
   `IPermissionCatalog`; the catalog is collected by assembly scan (Scrutor) and the
   dependency points feature → authorization (§11).
3. `AuthorizationSeeder` reconciles the database with the catalog at startup: it inserts new
   permissions, grants the `admin` role every permission, and grants each role listed in
   `PermissionDefinition.DefaultRoles` its own. Idempotent, safe on every start.
4. Permissions are checked only at the endpoint (`.HasPermission(...)`); handlers never check
   roles or permissions.
5. `IPermissionProvider.GetForUserIdAsync` loads a user's permissions in one query
   (join + `Distinct` + `AsNoTracking`), cached for 2 minutes under `auth:permissions:<userId>`
   (HybridCache, tag `auth:permissions`). A cache failure falls back to the database.
6. `InvalidateAsync(userId)` (or `RemoveByTagAsync("auth:permissions")`) must be called when a
   user's roles/permissions change; the TTL is only a safety net.
7. Fail closed: unauthenticated users are rejected and a lookup error denies access — access is
   never granted when permissions cannot be resolved.
8. `PermissionAuthorizationHandler` resolves the permission set once per request and memoizes it
   in `HttpContext.Items`.
9. `IPermissionProvider` is registered `Scoped` behind its interface so it can be faked in tests.
10. Multi-tenant (later): add the tenant column to `UserRoles` and filter the permission query
    by it.
11. Bootstrap: every registrant gets the `user` role and the first one additionally gets `admin`
    (`RegisterHandler`). Seeded roles: `admin` (all permissions), `user` (`todos:*`).
12. `GET /api/v1/identity/me` requires authentication but no permission and returns the caller's
    profile plus its effective permission names, so a client can hide UI it may not use. It reads
    the same `IPermissionProvider`, so the JWT still carries no permissions and the endpoint
    authority stays on the server.

JWT claims are only acceptable for a small, rarely changing permission set with short-lived
tokens (under 15 minutes); this template always reads from the database + cache.

## 11. Server rules: layering and entity placement

Infrastructure namespaces (`Authentication/`, `Authorization/`, `Database/`, `Common/`) are
consumed by features, never the other way around:

```text
Features ─► Authentication ─► Database ─► Common
Features ─► Authorization  ─► Database
Features ─► Database
```

1. An entity read by cross-feature infrastructure lives in `Database/Entities/Identity/`
   (namespace `AspNetVue.Server.Database.Entities.Identity`): `User`, `RefreshToken`, `Role`,
   `Permission`, `UserRole`, `RolePermission`. An entity used by a single feature stays in that
   feature root (`Features/Todos/TodoItem.cs`).
2. Features may use the identity entities, and hold use cases only — never entity definitions.
   `Features/Identity/` owns the credential/token lifecycle and the caller's own session
   (`Register`, `Login`, `Refresh`, `Me`); `Features/Users/` owns the user-record read model
   (`GetUsers`, `GetUserById`).
3. EF configurations follow their entity: identity configurations stay in
   `Database/Configurations/`, a feature configuration lives in the feature root
   (`Features/Todos/TodoItemConfiguration.cs`). `ApplyConfigurationsFromAssembly` finds both, so
   `Database/` never references `Features/`.
4. `ApplicationDbContext` declares `DbSet<>` only for entities it is allowed to reference
   (identity/access). Feature entities are reached with `context.Set<TodoItem>()` (or a
   feature-owned extension), not a `DbSet` property.
5. Identity entities stay minimal (`Id`, `Email`, `PasswordHash`, …). Feature-specific profile
   data goes into the feature's own entity keyed by `UserId`; columns are not added to `User`.
6. Enforced by `ArchitectureTests`: types in `Authentication`/`Authorization`/`Database`/`Common`
   must not depend on `AspNetVue.Server.Features`.
7. An error used by more than one feature lives in `Common/Errors/` (`UserErrors` is reported by
   the Todos ownership checks, the Users profile lookups, and the Identity login attempt). An
   error a single feature owns stays with it (`Features/Identity/IdentityErrors.cs`,
   `Features/Todos/TodoItemErrors.cs`). A feature never references another feature.
8. A route prefix matches its feature folder (`identity/register`, `users/{userId}`), so renaming
   or splitting a feature is an API change: update `README.md`, the OpenAPI tag
   (`Common/Endpoints/Tags.cs`), and the integration tests together.

Rule of thumb: if deleting a feature broke `Authorization/`'s compilation, the entity was not
owned by that feature.
