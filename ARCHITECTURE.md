# ARCHITECTURE

The server is a 1:1 port of the vertical-slice template (`Web.Api` → `AspNetVue.Server`)
with minimal adaptations for SPA hosting, file conventions, `Integrations/`,
and template distribution. The explicit list of deviations is in §8.

## 1. Vertical slices (`Features/*`)

One public type per file, prefixed with the use-case name:

```text
Features/{Domain}/{UseCase}/{UseCase}{Command,Validator,Handler,Endpoint,Request,Query,Response}.cs
```

Flat namespace `AspNetVue.Server.Features.{Domain}`. The prefix (not a sub-namespace)
is the disambiguation mechanism: every type name is globally unique (`RegisterCommand`
exists exactly once in the assembly), test references stay explicit, Go-to-Type
yields one result. Trade-off: ~50 files for 11 use-cases; renaming a use-case means
renaming the folder plus the prefixed types.

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
- Permission-based: `HasPermissionAttribute` → dynamic policy →
  `PermissionAuthorizationHandler` (as upstream: auto-succeeds any authenticated user,
  TODO to enforce real checks).
- EF Core + PostgreSQL 17, snake_case, `public` schema, domain-event dispatch
  post-commit (`Entity` → `DomainEventsDispatcher` via Scrutor `IDomainEventHandler<>`).
  2 bundled migrations; `ApplyMigrations()` stays commented (dev only), as upstream.

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
  (HMR); Vite fetches to `^/(todos|users|health)(/.*)?$` → Vite proxy → `:5000`
  (no CORS). No manual `UseSpa` (automatic via `SpaHostingStartup` +
  `spa.proxy.json` from the csproj: `SpaRoot`, `SpaProxyServerUrl`, `SpaProxyLaunchCommand`).
- Prod: `bun run build` in the client → straight into `../AspNetVue.Server/wwwroot/`
  (`vite.config.ts outDir`) → `dotnet publish` single artifact;
  `UseStaticFiles` + `MapFallbackToFile(index.html)` (Vue Router history mode).
- `wwwroot/` is a build artifact (gitignored), regenerated via the client build.
- Client route `/status` (not `/health`, which collides with server `GET /health`).
- API routes have no `/api` prefix (as upstream: `todos`, `users/*`, `health`).
- Unknown `todos/{**}` / `users/{**}` → `404 JSON` before the fallback,
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
Compose: `app` + `postgres:17` (DB `aspnetvue`) + `seq`; the app healthcheck uses `/health`.

## 8. Deviations from upstream (the only intentional ones)

1. Namespace `Web.Api*` → `AspNetVue.Server*`; compose DB `clean-architecture` → `aspnetvue`.
2. SPA hosting (§4): csproj SpaProxy props, `UseStaticFiles` + guards + fallback,
   Vite proxy regex, client route `/status`.
3. One prefixed type per file (§1) — zero behavior change.
4. `Notifications/` → `Integrations/Email/`; added `Integrations/Cache/` (§3).
5. `Testcontainers.PostgreSql` 4.12.0 → 4.16.0 (4.12 fails restore with NU1903 on SSH.NET
   + `TreatWarningsAsErrors`; upstream fails the same way).
6. Template packaging (§6 + `AspNetVue.Template.csproj`).
7. NOT ported (not functionality): `.claude/skills`, `.github/workflows`,
   `docker-compose.dcproj`, .NET 8/9 Dockerfiles, upstream slnx, upstream README.

## 9. Tests & rules

- Unit (`UnitTests`): handler tests (real `HybridCache` via `AddHybridCache()`,
  InMemory + NSubstitute) + `CacheServiceExtensionsTests` (default registers,
  Redis without a connection string throws).
- Integration (`IntegrationTests`): `WebApplicationFactory<Program>` +
  Testcontainers `postgres:17` (needs Docker Hub).
- Architecture (`ArchitectureTests`, NetArchTest): handlers + endpoints must be
  `sealed`; `Features` must not depend directly on `Database`.
- Build: CPM + `TreatWarningsAsErrors` (`Directory.Build.props` /
  `Directory.Packages.props`); `dotnet build` must report 0 warnings.
