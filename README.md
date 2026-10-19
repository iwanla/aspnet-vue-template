# AspNetVue — ASP.NET Core + Vue Application Template

A `dotnet new` template for starting an ASP.NET Core API and Vue SPA in one
repository, with vertical-slice application boundaries and a shared production
hosting artifact. Use the included application code as a starting point, not as
a fixed business-domain specification.

## What the template provides

- **Server:** .NET 10, MediatR-free CQRS, endpoint discovery, FluentValidation,
  Result/ProblemDetails handling, and URL-segment API versioning.
- **Security:** JWT authentication, refresh-token rotation, permission-based
  authorization, and rate limiting.
- **Persistence/integrations:** EF Core + PostgreSQL, migrations, post-save domain
  events, HybridCache with optional Redis, and an email integration boundary.
- **Client:** Vue 3, TypeScript, Vite, Pinia, and Vue Router.
- **Diagnostics:** Serilog, optional OpenTelemetry, health checks, and development
  OpenAPI/Scalar documentation.
- **Verification:** unit, PostgreSQL integration, and architecture tests.
- **Distribution:** name substitution, NuGet template packaging, and Docker assets
  to adapt for deployment.

## Prerequisites

- .NET 10 SDK.
- Bun 1.x.
- PostgreSQL, or Docker with Compose for the supplied local database service.
- Docker and access to pull `postgres:17` when running database integration tests.

Commands below use a POSIX shell. On PowerShell, adapt inline environment-variable
assignments to `$env:NAME = 'value'`.

## Create an application

Install from a local checkout; replace the path with this repository's absolute path:

```bash
dotnet new install /absolute/path/to/aspnet-vue-template
dotnet new aspnetvue -n SampleApp -o ./SampleApp
cd SampleApp
```

Alternatively, install a locally packed `.nupkg` (see [Packaging](#packaging-and-template-maintenance)).
If published to an accessible NuGet feed, the package can be installed with
`dotnet new install AspNetVue.Template`.

### Generated names and files

- `-n SampleApp` replaces `AspNetVue` in namespaces, project/folder names, solution
  references, and client build paths.
- Lowercase `aspnetvue` values become `sampleapp`, including the Compose database
  name, client package name, and cache prefix.
- NuGet restore runs after creation unless `--skipRestore` is supplied. Bun
  installation remains manual.
- Dependencies and generated output (`bin`, `obj`, `node_modules`, `dist`, server
  `wwwroot`, and TypeScript build artifacts) are excluded from scaffolding.
- The template packaging project itself is not included in generated applications.

## Run the generated application

The following commands assume the generated `SampleApp` root. When working directly
in this repository, use `AspNetVue` paths and database name `aspnetvue` instead.

Install client dependencies and start the supplied database:

```bash
(cd src/SampleApp.Client && bun install)
docker compose up -d postgres
```

In one terminal, start the API with the local Compose connection string:

```bash
ConnectionStrings__Database='Host=localhost;Port=5432;Database=sampleapp;Username=postgres;Password=postgres' \
  dotnet run --project src/SampleApp.Server --launch-profile http
```

In another terminal, start the client if Vite is not already running:

```bash
cd src/SampleApp.Client
bun run dev
```

The server has a SpaProxy launch command configured; do not launch a second Vite
instance if your host tooling already started it. The client uses a strict port.

| Service | Local URL |
| --- | --- |
| Vue client | `http://localhost:5173` |
| API | `http://localhost:5000` |
| Scalar API reference (Development only) | `http://localhost:5000/scalar/v1` |
| OpenAPI document (Development only) | `http://localhost:5000/openapi/v1.json` |
| Database health check | `http://localhost:5000/api/health` |

API requests under `/api` use the Vite proxy during development. Endpoint contracts
are available through Scalar/OpenAPI rather than maintained as a business-workflow
catalog in this README.

### Local configuration

The committed development database settings are machine-specific and do **not**
match the Compose database. Override `ConnectionStrings__Database` as above, or
configure your own PostgreSQL instance through user-secrets/environment variables.
Use `Host=localhost` for host processes; `Host=postgres` is for Compose networking.

Development startup applies migrations and attempts permission-catalog seeding.
Initialization errors are logged and caught, so check logs and database readiness
rather than treating a running process as proof of successful initialization.

Seq is optional: `docker compose up -d seq` exposes its UI on
`http://localhost:8081`. For a host-run API, override
`Serilog__WriteTo__1__Args__ServerUrl=http://localhost:8081`; the committed
`http://seq:5341` address is not the host-published ingestion URL.

## Repository layout

Paths shown here use the template's original name; generated applications use the
name supplied to `dotnet new`.

| Location | Purpose |
| --- | --- |
| `src/AspNetVue.Server/` | API, application slices, persistence, shared infrastructure, and SPA hosting. |
| `src/AspNetVue.Client/` | Browser UI, navigation, client state, and API consumption. |
| `tests/` | Unit, HTTP/database integration, and dependency-boundary tests. |
| `docs/references/` | Implementation references and practical development recipes. |
| `ARCHITECTURE.md` | Layer responsibilities, dependency rules, and execution guarantees. |
| `Directory.Build.props` | Shared .NET target framework and build/analyzer policy. |
| `Directory.Packages.props` | Central NuGet package versions. |
| `.template.config/` | Template metadata, substitutions, exclusions, and restore action. |
| `AspNetVue.Template.csproj` | NuGet template packaging; not part of the application solution. |
| `docker-compose.yml` | App/database/logging service topology. |

## Build and test

These commands run from this repository's root. For generated applications,
substitute the generated solution/project names.

```bash
dotnet build AspNetVue.slnx
dotnet test tests/AspNetVue.Server.UnitTests
dotnet test tests/AspNetVue.Server.ArchitectureTests
```

Database integration tests create their own PostgreSQL container, independently
of the Compose database:

```bash
dotnet test tests/AspNetVue.Server.IntegrationTests
```

`dotnet test AspNetVue.slnx` runs all three suites and therefore requires Docker.
For a focused test, append `--filter 'FullyQualifiedName~<ClassOrMethod>'` to the
relevant project command.

The client is not built or checked by the .NET solution:

```bash
cd src/AspNetVue.Client
bun run type-check
bun run build
```

There is no configured client test runner or lint script. .NET analyzer/style
warnings are treated as build errors.

## Publish and deployment

Build the client **before** publishing the server. From the repository root:

```bash
(cd src/AspNetVue.Client && bun run build)
dotnet publish src/AspNetVue.Server -c Release
```

Vite empties and writes `src/AspNetVue.Server/wwwroot`; keep hand-maintained source
out of that directory. Plain `dotnet publish` does not run the client build.
The default publish output is under `src/AspNetVue.Server/bin/Release/net10.0/publish/`.

Before production use:

- Supply database and `Jwt` settings externally, including a private signing secret,
  issuer, audience, and token expiration. Base settings are empty; development
  credentials/secrets are not production configuration.
- Apply database migrations separately; automatic migrations are Development-only.
- Review authorization bootstrap/default grants, refresh-token handling, rate
  limits, CORS, and integration providers for your application's requirements.
- Adapt the Docker assets before relying on them: the current Dockerfile's publish
  stage does not copy root `Directory.Build.props`/`Directory.Packages.props`, and
  Compose does not supply production JWT settings or apply production migrations.
  The multi-stage build is intended to build the client inside the image.

## Packaging and template maintenance

From the template repository root:

```bash
dotnet pack AspNetVue.Template.csproj -c Release
```

Install the resulting package using its actual path, for example:
`dotnet new install ./bin/Release/AspNetVue.Template.<version>.nupkg`.
Package identity/version are defined in `AspNetVue.Template.csproj`.

Keep packaging exclusions and `.template.config/template.json` exclusions aligned.
Check scaffolding with a different application name after changing paths,
namespaces, configuration, or template content; verify the generated app, not only
the source repository. Avoid creating smoke-test output inside template content.

To uninstall, use the installation source:

```bash
# Folder installation
dotnet new uninstall /absolute/path/to/aspnet-vue-template

# NuGet package installation
dotnet new uninstall AspNetVue.Template
```

## Development documentation

- [Application architecture](ARCHITECTURE.md): stable layer boundaries and design.
- [Feature development guide](docs/references/feature-development.md): step-by-step
  templates for implementing server use cases without copying an existing feature.
- [Persistence](docs/references/persistence.md): mappings, auditing, soft delete, and domain events.
- [Authentication and authorization](docs/references/authentication-authorization.md): permission resolution, caching, and initialization.
- [Server localization](docs/references/server-localization.md): RESX resources, culture selection, and fallback.
- [Client HTTP](docs/references/client-http.md): shared transport usage and response/error contracts.
- [Repository agent instructions](AGENTS.md), [server instructions](src/AspNetVue.Server/AGENTS.md),
  and [client instructions](src/AspNetVue.Client/AGENTS.md): scoped conventions and
  focused verification commands.
