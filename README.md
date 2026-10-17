# AspNetVue — ASP.NET Core base (vertical slice) + Vue SPA Proxy

Server ported 1:1 from the vertical-slice template (MediatR-free CQRS, JWT + refresh rotation,
permission-based authz, EF Core + PostgreSQL, HybridCache, rate limiting, OTel, Serilog,
Scalar/OpenAPI, ProblemDetails) + SPA Proxy hosting for `AspNetVue.Client` (Vue 3 + Vite + TS).

## Install as a template (`dotnet new`)

```bash
# From a local folder
dotnet new install /path/to/aspnet-vue-template

# Or from a NuGet package (after `dotnet pack` + push)
dotnet new install AspNetVue.Template

# Use it
dotnet new aspnetvue -n SampleApp
cd SampleApp/src/SampleApp.Client && bun install
cd ../SampleApp.Server && dotnet run

# Uninstall
dotnet new uninstall AspNetVue.Template
```

`-n` renames `AspNetVue` → the new name (namespaces, folders, slnx, `vite.config.ts`
`outDir`) and `aspnetvue` → its lowercase form (compose DB name, package name,
`Cache:KeyPrefix`). `--skipRestore` skips the automatic NuGet restore.
`bin/`, `obj/`, `node_modules/`, and build output (`wwwroot/`, `dist/`) are excluded
from scaffolding; `wwwroot/` is regenerated via `bun run build` in the client.

## Structure

```text
├── AspNetVue.slnx
├── Directory.Build.props / Directory.Packages.props  ← CPM + warnings-as-errors
├── src/
│   ├── AspNetVue.Server/            ← ASP.NET Core (vertical-slice port)
│   │   ├── Authentication/      ← JWT, password hasher, token provider, user context
│   │   ├── Authorization/      ← RBAC: policy/handler/provider, permission catalog + seeder
│   │   ├── Common/             ← Result/Error + shared errors, behaviors, endpoints, messaging
│   │   ├── Database/           ← DbContext, Entities/Identity/, configs, migrations
│   │   ├── Features/Todos|Users|Identity/ ← vertical slices (endpoint + handler + validator)
│   │   ├── Integrations/Email|Cache/ ← IEmailSender, HybridCache + Redis option
│   │   ├── Program.cs / DependencyInjection.cs
│   │   └── wwwroot/            ← client build output (gitignored)
│   └── AspNetVue.Client/           ← Vue (src/{app,features,shared}/)
├── tests/
│   ├── AspNetVue.Server.UnitTests/         ← handler tests (InMemory + NSubstitute)
│   ├── AspNetVue.Server.IntegrationTests/  ← Testcontainers postgres:17
│   └── AspNetVue.Server.ArchitectureTests/ ← NetArchTest (sealed handlers/endpoints, layering, feature isolation)
├── README.md / ARCHITECTURE.md   ← architecture + decisions (no ADR folder)
└── docker-compose.yml                  ← app + postgres + seq
```

Features depend on `Authentication/`, `Authorization/`, `Database/`, `Common/` — never the
reverse. Identity entities used by that infrastructure live in `Database/Entities/Identity/`
(see `ARCHITECTURE.md` §11).

## Run (dev)

Requires: .NET 10 SDK, Bun 1.x, Docker (for local postgres).

```bash
docker compose up -d postgres        # or use a local postgres
cd src/AspNetVue.Client && bun install
cd ../AspNetVue.Server && dotnet run    # auto-runs `bun run dev` via SpaProxy
```

- API: `http://localhost:5000` · Scalar docs: `http://localhost:5000/scalar/v1`
- Vite: `http://localhost:5173` (proxies `/api/*` → `:5000`)
- Seq: `http://localhost:8081` (logs from `appsettings.Development.json`)

> `appsettings.Development.json` points at `Host=postgres` (the compose service name).
> Without compose, override via user-secrets / env: `ConnectionStrings__Database`.

## Endpoints

| Method | Route | Auth |
|---|---|---|
| POST | `api/v1/identity/register` | anon, rate-limit `authentication` |
| POST | `api/v1/identity/login` | anon, rate-limit `authentication` → `{ accessToken, refreshToken }` |
| POST | `api/v1/identity/refresh` | anon, rate-limit `authentication` |
| GET | `api/v1/identity/me` | login (any authenticated user) → profil + permissions efektif |
| GET | `api/v1/users` | permission `users:access` (paged, `search`/`sortColumn`/`sortOrder`) |
| GET | `api/v1/users/{userId}` | permission `users:access` |
| POST / GET / GET{id} / PUT / DELETE / PUT complete / POST copy | `api/v1/todos…` | permissions `todos:read/create/update/delete` |
| GET | `api/health` | anon (Npgsql check) |
| GET | `/scalar/v1`, `/openapi/v1.json` | dev only |

Non-API routes matching no static file → `index.html` (Vue Router history mode);
unknown `api/{**}` → `404 JSON`.

Business endpoints use URL-segment API versioning (`Asp.Versioning`): `/api/v1/...`.
The version segment is required; `/api/todos` and unsupported versions return 404.
Health remains unversioned at `/api/health`. OpenAPI substitutes `v1` in paths.
To add v2, declare it on the version set in `Program.cs`, map the relevant
endpoints with `.MapToApiVersion(2)`, and register a `v2` OpenAPI document.
Existing endpoints remain v1 unless explicitly mapped to another version.

## Authorization (RBAC)

Permission-based: the database is the source of truth, the JWT only carries identity (`sub`).

- Model: `users —< user_roles >— roles —< role_permissions >— permissions`.
- The entities live in `Database/Entities/Identity/` (`User`, `RefreshToken`, `Role`,
  `Permission`, `UserRole`, `RolePermission`) because `Authentication/` and `Authorization/`
  consume them; features use them, not the other way around (`ARCHITECTURE.md` §11).
- Permissions are constants owned by the feature that declares them
  (`Features/Todos/TodosPermissions.cs`, `Features/Users/UsersPermissions.cs`, format
  `<feature>:<action>`) and collected by assembly scan (`IPermissionCatalog`).
- `AuthorizationDataInitializer` migrates in Development and seeds the catalog on every start:
  the `admin` role gets every permission, `user` gets `todos:*`.
- Bootstrap: the first registrant additionally becomes `admin`, every registrant gets `user`
  (`Features/Identity/Register/RegisterHandler.cs`). Endpoints check permissions only
  (`.HasPermission(...)`); handlers never check roles.
- The credential/token endpoints under `/api/v1/identity/*` are anonymous; the user-record
  endpoint `/api/v1/users/{userId}` requires `users:access`.
- `GET /api/v1/identity/me` needs only a valid token (no permission) and returns the caller's
  profile plus its **effective permission names**, so a client can hide UI it may not use. It
  reads the same `IPermissionProvider` (2-minute cache) — the JWT still carries no permissions
  and the server remains the only authority (hiding a button is UX, not security).
- `IPermissionProvider` reads a user's permissions in one query and caches them for 2 minutes
  (`auth:permissions:<userId>`); access is denied when permissions cannot be resolved.

Add a permission: create `Features/{Domain}/{Domain}Permissions.cs` + an `IPermissionCatalog`,
call `.HasPermission(...)` in the endpoint, and restart — the seeder reconciles the database.
Full rules: `ARCHITECTURE.md` §10 (RBAC) and §11 (layering).

## Tests

```bash
dotnet test                        # unit (40) + architecture (4)
dotnet test tests/AspNetVue.Server.IntegrationTests  # needs Docker Hub (pulls postgres:17)
```

## Prod / Docker

```bash
cd src/AspNetVue.Client && bun run build   # outputs directly to ../AspNetVue.Server/wwwroot
docker compose up --build
# app http://localhost:5000, seq http://localhost:8081
```

> Plain `dotnet publish` ships whatever is in `wwwroot/` — it does not build the
> client. Always run `bun run build` first (Docker builds the client itself
> inside the image, so `docker compose up --build` needs no manual step).

## Differences from upstream

- Namespace `Web.Api` → `AspNetVue.Server`; test assemblies `AspNetVue.Server.{Unit,Integration,Architecture}Tests`.
- Added: SpaProxy (`SpaRoot`, `SpaProxyServerUrl`), `UseStaticFiles` + guards + `MapFallbackToFile`.
- All API routes under the `/api` prefix (upstream has none): versioned business group `api/v{version:apiVersion}` + unversioned `api/health`; Vite proxies `/api`; client fetches `/api/health`.
- `Testcontainers.PostgreSql` 4.12.0 → 4.16.0 (4.12's transitive `SSH.NET` hits NU1903 very-high; upstream fails to build the same way right now).
- Compose DB `clean-architecture` → `aspnetvue`.
