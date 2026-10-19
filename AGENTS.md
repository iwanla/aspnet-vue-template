# AGENTS.md

## Repository Structure

Use these boundaries when deciding where a change belongs:

| Location | Responsibility / what belongs here |
| --- | --- |
| `src/AspNetVue.Server/` | Server-side use cases, persistence, authentication/authorization, and API contracts. Business enforcement belongs here, not in the SPA. |
| `src/AspNetVue.Client/` | Browser UI, navigation, presentation state, and API consumption. Keep server secrets and database access out. |
| `tests/AspNetVue.Server.UnitTests/` | Isolated handler/service behavior with controlled dependencies; not proof of PostgreSQL behavior. |
| `tests/AspNetVue.Server.IntegrationTests/` | HTTP contracts, authentication/authorization wiring, and real PostgreSQL/migration behavior. |
| `tests/AspNetVue.Server.ArchitectureTests/` | Executable dependency and type-shape rules; extend these when adding architectural boundaries. |
| `.template.config/template.json` | Scaffolding rules: name substitutions, excluded files, and post-creation actions. |
| `AspNetVue.Template.csproj` | NuGet template packaging and shipped content; keep packaging exclusions aligned with scaffolding. |
| `AspNetVue.slnx` | .NET project membership; client and template packaging remain separate build concerns. |
| `Directory.Build.props` | Shared .NET target framework, compiler, and analyzer policy. |
| `Directory.Packages.props` | Central NuGet version ownership; do not scatter versions across projects. |
| `docker-compose.yml` | Local service topology, networking, ports, and persistent volumes. |
| `ARCHITECTURE.md` | Application boundaries, dependency rules, and execution guarantees; not detailed implementation recipes. |
| `docs/references/` | Implementation mechanisms and recipes; `feature-development.md` explains how to build server features/use cases. |

## Scope and setup

- Use .NET 10 SDK and Bun 1.x. Scaffolding restores NuGet, not Bun dependencies.
- Subproject instructions: `src/AspNetVue.Server/AGENTS.md` and `src/AspNetVue.Client/AGENTS.md`. For changes under `tests/`, also read the server instructions; read both subproject files when changing API/client contracts.
- `ARCHITECTURE.md` documents layer responsibilities, dependency boundaries, persistence/event semantics, and extension rules; tooling quirks belong in subproject instructions.

## Cross-project verification

- From the repo root: `dotnet build AspNetVue.slnx`; `dotnet test AspNetVue.slnx`. The test command includes Docker-backed integration tests.
- The .NET solution does not build/check the client. In `src/AspNetVue.Client`, run `bun run type-check` and `bun run build` separately.
- Run the client build before `dotnet publish`: publishing does not build the SPA. Vite empties and writes server `wwwroot`; never keep source files there.
- NuGet versions belong in `Directory.Packages.props`, not individual `.csproj` files. Analyzer and `.editorconfig` violations fail .NET builds.

## Template distribution

- Pack with `dotnet pack AspNetVue.Template.csproj`; the pack project is outside the solution.
- Preserve `AspNetVue`/`aspnetvue` substitutions in `.template.config/template.json`. Keep its artifact exclusions aligned with `AspNetVue.Template.csproj` so generated output and dependencies do not ship.
