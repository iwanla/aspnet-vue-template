# Application Architecture

## Purpose and scope

This document defines application boundaries: what each layer owns, how layers
collaborate, and where new code belongs. It describes the implemented architecture,
not a proposed migration to a different architectural style.

Business workflows belong to their feature code and API documentation. Adding,
renaming, or removing an ordinary feature should not require updating this document.
Update it when responsibilities, dependency rules, shared contracts, execution
semantics, or deployment boundaries change. Setup commands and operational settings
belong in `README.md` and the root/subproject `AGENTS.md` files.

## Architectural model

The application has a browser client and an ASP.NET Core server. The server uses
vertical slices: an operation's HTTP boundary, application logic, and domain-specific
types are kept close together instead of distributed across global controller,
service, and repository folders.

The layers below are **logical responsibilities**, not separate .NET projects.
Endpoints, handlers, domain types, and infrastructure currently share one server
assembly. In particular, handlers use EF Core directly; this is not a strict Clean
Architecture arrangement with a persistence-independent application project.

```text
Browser client -- HTTP/JSON --> Server presentation boundary
                                      |
                                Application handler
                                  /       |       \
                         Domain types  Persistence  Integration services
```

The composition root wires these responsibilities together. Authentication,
authorization, validation, logging, and error translation support the flow without
owning individual business processes.

## Dependency boundaries

The following are permitted dependency directions, not an exhaustive class graph:

```text
Composition root --> Features and shared infrastructure registrations
Features         --> Authentication, Authorization, Database, Integrations, Common
Authentication   --> Database, Common
Authorization    --> Authentication, Database, Common
Database         --> Common
```

- A feature must not reference another feature's types. Shared errors/contracts
  belong in a feature-independent location only when genuinely shared.
- `Authentication`, `Authorization`, `Database`, and `Common` must not reference
  `Features`. Generic contracts and assembly scanning let infrastructure consume
  feature implementations without importing their concrete types.
- Integration adapters should remain feature-independent; application handlers
  supply business inputs and decide when to invoke them.
- `Common` must not become a service locator or a container for business workflows.
- `Common` must not depend on other server areas, including `Database`,
  `Authentication`, `Authorization`, `Integrations`, and `Hosting`. Host composition
  helpers that connect those areas belong in the server-root `Hosting/` folder.
- Client/server interaction is through HTTP contracts, not shared runtime objects
  or direct browser access to persistence.

Domain types are kept local to their owner; logical layering does not imply that
every domain needs another assembly, service abstraction, or repository wrapper.

## Composition root

**Location:** `Program.cs`, `DependencyInjection.cs`, and server-root `Hosting/`.

The composition root selects implementations, configures lifetimes, installs
cross-cutting services, and connects routes and middleware. It must not implement
business workflows.

Shared services depend on contracts or generic discovery mechanisms. Their source
code should remain unchanged when an ordinary feature is added or removed.
Endpoint, handler, mapping, and permission-catalog discovery connects ordinary
slices without feature-specific startup branches.

`Hosting/` contains helpers that connect server areas. Capability-specific service
registration stays with its capability. Registration groups, discovery, and
decorator composition are described in the
[Feature Development Guide](docs/references/feature-development.md#shared-capabilities-and-composition).

## Presentation boundary

**Location:** feature-owned endpoint/request/response types; shared HTTP mechanisms
in `Common/Endpoints`, `Common/Results`, `Common/OpenApi`, and `Common/Middleware`.

The presentation boundary owns HTTP concerns: binding input, declaring routes,
version metadata and access requirements, invoking a handler, and translating its
result into an HTTP response. It must not own persistence operations or duplicate
the handler's business decisions.

Endpoints invoke command/query handler interfaces directly, without a mediator.
Handlers return `Result`/`Result<T>`; endpoints translate expected failures into
HTTP responses. Unexpected exceptions use the global exception handler.
Authentication challenges and authorization failures remain framework concerns.

The host groups business endpoints under a versioned `/api` boundary. Health is
unversioned at `/api/health`. Unknown API routes return JSON 404s rather than the
SPA document; non-API navigation may use the client-side routing fallback.
Introducing an API version is an explicit host/endpoint/documentation decision,
not a side effect of adding a feature.

## Application layer

**Location:** `Features/{Domain}/{UseCase}/`; messaging and decorator contracts in
`Common/Messaging` and `Common/Behaviors`.

An application handler coordinates one operation: validate business preconditions,
obtain the current user's identity when needed, load state, invoke domain behavior,
persist changes, invoke integrations, and return an outcome. It should not construct
HTTP responses, read transport headers, or make decisions about UI rendering.

- Commands express state-changing intent; queries express read intent and project
  data into response models. They use the same persistence infrastructure; separate
  read/write databases are not part of this architecture.
- FluentValidation validators belong to the operation and check command input.
  State-dependent invariants and ownership checks still belong in handlers/domain
  behavior; input validation alone does not establish permission to access a record.
- An operation may use `ApplicationDbContext` and `HybridCache` directly. Do not
  describe these handlers as infrastructure-independent or introduce a mandatory
  repository layer that the application does not have.

Use-case-prefixed types live together under `{Domain}/{UseCase}`. Supporting
domain-owned types shared by that domain's operations live at the domain root.
Neither the host nor shared infrastructure should import those concrete types.

## Domain ownership and shared kernel

**Locations:** `Features/{Domain}/` for domain-owned types;
`Database/Entities/Identity/` for shared identity/access entities; `Common/` for
feature-independent foundations.

Domain entities and events represent state and business meaning. Handlers coordinate
them; endpoints translate transport concerns. Domain behavior should not depend on
HTTP context, client components, or provider-specific integration details.

Ownership determines placement:

| Type of concern | Owner and placement |
| --- | --- |
| Entity, event, error, or cache-key scheme used by one domain | That domain's feature root. |
| EF mapping for a feature entity | Beside that entity, discovered through configuration scanning. |
| Identity/access entity needed by shared authentication or authorization | `Database/Entities/Identity/`; keep it minimal. |
| Feature-specific data about a user | A feature-owned entity keyed by user ID, not additional business fields on the shared identity entity. |
| Error used across feature boundaries | `Common/Errors/`, without importing feature types. |
| Generic result, event, time, or messaging contract | `Common/`, without business workflow knowledge. |

Organize `Common/` by capability, with no source types directly in its root.
Keep capability-specific extensions local; general type helpers must remain pure,
without wiring or business rules. The capability inventory and compatibility notes
belong in the [Feature Development Guide](docs/references/feature-development.md#shared-capabilities-and-composition).

Do not move business logic into shared infrastructure merely to make it callable
from another feature. A new cross-feature collaboration requires an explicit
contract and dependency decision, not a direct reference to another feature's
handler or entity.

## Persistence and domain events

**Location:** `Database/`, with feature entity mappings owned by their features.

`ApplicationDbContext` owns EF persistence orchestration, model discovery, and
domain-event dispatch after saving. PostgreSQL is the durable source of truth;
migrations describe versioned schema transitions.

Feature entities are accessed generically and their mappings are discovered, so
the context does not depend on concrete feature types.

Domain-event delivery is in-process, post-save dispatch, not a transactional outbox
or durable queue.
Event handlers do not share the originating context's unit of work. If dispatch
fails after the database save succeeds, persisted data is not automatically rolled
back, and delivery/retry is not guaranteed. Do not rely on it for atomic external
side effects without changing the persistence/delivery design explicitly.

Auditing and soft delete are persistence responsibilities, not per-handler
workflows. Audited entities are hidden from normal reads after deletion, and their
unique values can be reused. Access joins remain hard-deleted and their query
visibility follows the active identity/access records. Business reads must not
bypass soft-delete filters.

See [Persistence](docs/references/persistence.md) for mappings, audit stamping,
query filters, partial indexes, and the event-dispatch sequence.

## Authentication and authorization

**Locations:** `Authentication/`, `Authorization/`; credential workflows remain
application use cases rather than infrastructure responsibilities.

Authentication establishes identity. Token/password mechanisms live behind shared
interfaces; application code obtains the caller through `IUserContext` rather than
parsing HTTP claims independently. JWTs carry identity, not permission grants.

Authorization resolves endpoint permission requirements against database-backed
role membership and grants.

- Features own permission constants and implement `IPermissionCatalog` using shared
  `PermissionDefinition` contracts. Assembly discovery keeps `Authorization`
  independent of concrete features.
- The database holds role membership and permission grants. Roles group grants;
  endpoints check permissions, not role names. Record ownership and business
  preconditions remain application responsibilities even after a permission passes.
- Membership/grant mutations must invalidate affected permission caches; expiration
  alone is not a substitute for invalidation. Permission lookup failures deny access.

See [Authentication and Authorization](docs/references/authentication-authorization.md)
for policy resolution, cache behavior, additive seeding, and initialization failures.
Production migrations/configuration are deployment responsibilities; a running
host is not proof of successful database initialization.

## Integrations and cross-cutting infrastructure

**Locations:** `Integrations/` for adapters/configuration; `Common/` for shared
execution mechanisms. Business decisions about using them remain with the caller.

| Concern | Responsibility and boundary |
| --- | --- |
| Cache | HybridCache is the shared API, with memory caching and optional Redis backing. Configuration belongs to the integration; key ownership, authorization-safe scoping, and invalidation belong to the data owner. Cache is not the durable source of truth. |
| Email | Application code uses `IEmailSender`; delivery-provider details stay behind its implementation. Logging delivery is not a guarantee of external message delivery. |
| Idempotency | An opt-in endpoint filter remembers supplied keys and rejects repeats. It is cache-backed duplicate rejection, not durable exactly-once processing or response replay. The marker is written before the operation finishes. |
| Rate limiting | Host/middleware policies protect request capacity; limits do not replace authentication, permissions, or business rules. |
| Observability | Structured/request logging, handler decorators, and optional telemetry report execution; they must not become workflow dependencies. |
| Exception handling | The HTTP boundary translates unexpected failures; expected application outcomes use `Result`/`Error`. |

New providers should implement the relevant integration boundary and be wired at
composition time, rather than embedding provider-specific behavior into endpoints.

## Server localization

**Location:** `Common/Localization/`; feature-owned `{Domain}Resources.resx` files.

The server uses built-in .NET RESX localization for server-generated text such as email,
PDFs, and notifications. UI translations remain client-owned; resource files are not
shared merely because their text is similar. Existing API error codes/descriptions
are not translated by this infrastructure.

Shared infrastructure selects request culture; features own resource content.
Culture selection does not imply a persisted user preference. Resource layout,
supported cultures, lookup/fallback, and verification are documented in
[Server Localization](docs/references/server-localization.md).

## Client layers and hosting boundary

**Location:** `src/AspNetVue.Client/`.

| Boundary | Responsibility | Must not own |
| --- | --- | --- |
| Bootstrap (`src/main.ts`) | Create the app and install shared plugins. | Feature workflows. |
| App (`src/app/`) | Application shell, routing composition, and global styling. | Feature-specific business behavior. |
| Features (`src/features/{feature}/`) | Views, local presentation state, component contracts, and feature-specific API interactions. | Server-side authorization or database rules. |
| Shared (`src/shared/`) | Feature-independent transport/UI utilities and reusable logic. | Dependencies on concrete feature implementations. |

Keep state near the UI that owns it; shared state is justified by consumers across
component boundaries, not by a requirement to put all state in a global store.
Typed API models describe expectations but do not validate JSON at runtime.
Client permissions control presentation only; the server remains authoritative.

Client API calls use the shared fetch transport in `src/shared/api/http.ts`.
Transport owns serialization, timeout, and cancellation, not session management,
navigation, retry policy, or business error presentation. Response/error contracts
and usage are documented in [Client HTTP](docs/references/client-http.md).

Development uses the client dev server's `/api` proxy to reach the API. Production
builds client assets into server `wwwroot`, then the server serves static files and
the history-routing fallback. This shares a deployment artifact, not business
logic. `wwwroot` is generated output; publishing the server does not itself build
the client.

## Extending the application without changing its architecture

For an ordinary use case, add its endpoint, messages, handler, validator, and response
types within the owning feature. Add domain types, mappings, permission catalogs,
and integration calls only where that feature needs them. Generic discovery should
connect them without feature-specific changes to shared infrastructure.

For the implementation recipe and complete command/query templates, read
[Feature Development Guide](docs/references/feature-development.md).

Changing a workflow, granting a new capability, adding a table, or adding a page
does not by itself change these layer responsibilities. Document workflow details
with the feature/API, not as an inventory in this document.

Revisit this architecture when introducing a new dependency direction, shared
cross-feature contract, deployment process boundary, persistence abstraction,
authorization model, or transaction/event-delivery guarantee. Document what boundary
changed, why it changed, and how enforcement changes with it.

## Verification and source of truth

- Architecture tests enforce sealed endpoint/handler types, infrastructure-to-feature
  dependency prohibitions, capability namespaces for `Common` types, no dependencies
  from `Common` to other server areas, and isolation between the features listed by the test.
  The feature list is currently explicit; adding a feature requires extending that
  coverage. These tests do not enforce every responsibility described above.
- Unit tests verify application/service behavior with controlled dependencies.
  PostgreSQL integration tests verify relational behavior and HTTP/security wiring;
  InMemory tests do not establish production persistence semantics.
- Client checks run independently of the .NET solution. Build and test commands,
  tooling quirks, and environment prerequisites live in the relevant `AGENTS.md`.

Implementation and executable tests remain the source of truth for current
behavior. References explain mechanisms and recipes; this document owns the
application's boundaries and guarantees. Update the relevant reference when a
mechanism changes, and this document when its architectural contract changes.
