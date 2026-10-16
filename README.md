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
│   │   ├── Authorization/      ← permission policy/handler/provider
│   │   ├── Common/             ← Result/Error, behaviors, endpoints, middleware, messaging
│   │   ├── Database/           ← DbContext, configs, migrations
│   │   ├── Features/Todos|Users/ ← vertical slices (endpoint + handler + validator)
│   │   ├── Integrations/Email|Cache/ ← IEmailSender, HybridCache + Redis option
│   │   ├── Program.cs / DependencyInjection.cs
│   │   └── wwwroot/            ← client build output (gitignored)
│   └── AspNetVue.Client/           ← Vue (src/{app,features,shared}/)
├── tests/
│   ├── AspNetVue.Server.UnitTests/         ← handler tests (InMemory + NSubstitute)
│   ├── AspNetVue.Server.IntegrationTests/  ← Testcontainers postgres:17
│   └── AspNetVue.Server.ArchitectureTests/ ← NetArchTest (sealed handlers/endpoints)
├── README.md / ARCHITECTURE.md   ← architecture + decisions (no ADR folder)
└── docker-compose.yml                  ← app + postgres + seq
```

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
| POST | `api/users/register` | anon, rate-limit `authentication` |
| POST | `api/users/login` | anon, rate-limit `authentication` → `{ accessToken, refreshToken }` |
| POST | `api/users/refresh-token` | anon, rate-limit `authentication` |
| GET | `api/users/{userId}` | permission `users:access` |
| POST / GET / GET{id} / PUT / DELETE / PUT complete / POST copy | `api/todos…` | JWT |
| GET | `api/health` | anon (Npgsql check) |
| GET | `/scalar/v1`, `/openapi/v1.json` | dev only |

Non-API routes matching no static file → `index.html` (Vue Router history mode);
unknown `api/{**}` → `404 JSON`.

## Tests

```bash
dotnet test                        # unit (28) + architecture (2)
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
- All API routes under the `/api` prefix (upstream has none): `MapEndpoints(app.MapGroup("api"))` + `api/health`; Vite proxies `^/api(/.*)?$`; client fetches `/api/health`.
- `Testcontainers.PostgreSql` 4.12.0 → 4.16.0 (4.12's transitive `SSH.NET` hits NU1903 very-high; upstream fails to build the same way right now).
- Compose DB `clean-architecture` → `aspnetvue`.
