# Feature Development Guide

This is the implementation recipe for server features. Read it before adding or
changing a use case; reading another feature first is not required. For the reasons
behind these rules, see [Application Architecture](../../ARCHITECTURE.md).

All shell commands run from the repository root. Server paths below are relative
to `src/AspNetVue.Server/` unless stated otherwise. The **Notes** domain is a worked
example, not an implemented feature or a request to scaffold it automatically.

## 1. Choose ownership and create only the needed files

Add an operation to the domain that owns its data and behavior. Create a new domain
only for a distinct responsibility, not for every endpoint. Do not import another
feature's commands, handlers, entities, or errors.

```text
Features/{Domain}/
  {Entity}.cs                     Feature-owned state
  {Entity}Configuration.cs        EF mapping; only when persistence is needed
  {Domain}Errors.cs               Expected domain failures
  {Domain}Permissions.cs          Permission constants
  {Domain}PermissionCatalog.cs    Permission definitions discovered at startup
  {UseCase}/
    {UseCase}Endpoint.cs          HTTP binding, access metadata, result translation
    {UseCase}Request.cs           HTTP body contract, when needed
    {UseCase}Command.cs           State-changing intent; OR a Query, not both
    {UseCase}Query.cs             Read intent, when needed
    {UseCase}Validator.cs         Command input rules, when needed
    {UseCase}Handler.cs           Operation orchestration and business checks
    {UseCase}Response.cs          Response projection, when needed
```

Use one top-level type per file and a matching filename. Namespaces follow folders:
`AspNetVue.Server.Features.{Domain}.{UseCase}`. Keep use-case prefixes on types.
Do not create empty request/response/validator types just to fill out the tree.

| Responsibility | Implementation rule |
| --- | --- |
| Endpoint | Public sealed `IEndpoint`; map relative routes, construct the message, invoke its handler interface, translate `Result`. No EF access. |
| Request | Transport data only. Never accept caller identity as authoritative merely because it is in the body. |
| Command | Implement `ICommand` for no response payload, or `ICommand<TResponse>` for a payload. |
| Query | Implement `IQuery<TResponse>`; project a read model without changing business state. |
| Validator | Derive from `AbstractValidator<TCommand>`. Validate shape/ranges; leave database invariants and record ownership to the handler. |
| Handler | Internal sealed implementation of the matching handler interface. Inject dependencies, pass cancellation tokens to I/O, return expected errors as results. |
| Response | An explicit API projection, not an EF entity or `IQueryable`. |

### Shared capabilities and composition

Organize `Common/` by capability; no source types belong directly in its root:

- `Domain/`: `Entity`, `BaseEntity`, and domain-event contracts. `Entity` supplies
  event collection; `BaseEntity` adds identity, auditing, and soft-delete state.
- `Results/`: results, errors, validation errors, pagination, and result/HTTP helpers.
- `Messaging/`: command/query markers and handler contracts, without a mediator.
- `Behaviors/`: logging and validation handler decorators.
- `Endpoints/` and `Middleware/`: shared HTTP mechanisms.
- `Localization/`, `OpenApi/`, `RateLimiting/`, `Telemetry/`, and `Time/`: their
  respective capabilities, including their capability-specific extensions.
- `Extensions/`: pure type helpers (`StringExtensions` and `IEnumerableExtensions`),
  not wiring or business rules. Prefer built-in string/LINQ operations when sufficient.

User-record errors belong to `Features/Users/UserErrors`; credential/session errors
belong to `Features/Identity/IdentityErrors`; Todo precondition errors belong to
`Features/Todos/TodoItemErrors`. The latter two retain legacy `Users.*` codes,
descriptions, and error types for API compatibility; those strings do not imply a
dependency on Users. Create `Common/Errors/` only for domain-neutral shared errors.

`DependencyInjection.cs` composes three registration groups:

- `AddApplication()` discovers scoped command/query handlers, validators, and
  domain-event handlers, then applies handler decorators. Validation is registered
  before logging so logging wraps validation. Queries have logging but no automatic
  validation decorator.
- `AddPresentation()` configures HTTP services such as exception handling and API
  versioning. Endpoints implement `IEndpoint` and are assembly-scanned.
- `AddInfrastructure()` connects persistence, authentication, authorization, and
  integrations to configuration, including permission-catalog discovery.

Server-root `Hosting/` contains composition helpers for middleware, migrations,
OpenAPI route mapping, and CORS. Capability-specific registration stays with its
capability; ordinary features must not require branches in shared startup code.

## 2. Define feature-owned state and mappings

The example stores a caller-owned note. It uses the shared identity entity only
as a foreign-key target; it does not add note fields to the shared user entity.

**`Features/Notes/Note.cs`**

```csharp
using AspNetVue.Server.Common.Domain;

namespace AspNetVue.Server.Features.Notes;

public sealed class Note : BaseEntity
{
    public Guid UserId { get; set; }
    public string Title { get; set; } = string.Empty;
}
```

**`Features/Notes/NoteConfiguration.cs`**

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AspNetVue.Server.Database.Entities.Identity;

namespace AspNetVue.Server.Features.Notes;

internal sealed class NoteConfiguration : IEntityTypeConfiguration<Note>
{
    public void Configure(EntityTypeBuilder<Note> builder)
    {
        builder.ToTable("notes");
        builder.HasKey(note => note.Id);
        builder.Property(note => note.Title).HasMaxLength(200).IsRequired();
        builder.HasOne<User>().WithMany().HasForeignKey(note => note.UserId);
    }
}
```

Use `context.Set<Note>()`, not a new feature `DbSet` in `ApplicationDbContext`.
The context scans configurations automatically. Explicit table names prevent a
change in DbSet ownership from unexpectedly renaming a physical table.

`BaseEntity` supplies `Id` plus the create/update/delete audit columns; do not
redeclare them. Deletes are soft by default: `Remove` records `DeletedAt` and the
global query filter hides the row, so write `DeleteNote` like any other delete and do
not add `IsDeleted` checks to queries.

See [Persistence](persistence.md) for mapping discovery, audit stamping, uniqueness,
join visibility, and domain-event delivery details.

**`Features/Notes/NotesErrors.cs`**

```csharp
using AspNetVue.Server.Common.Results;

namespace AspNetVue.Server.Features.Notes;

public static class NotesErrors
{
    public static Error NotFound(Guid id) =>
        Error.NotFound("Notes.NotFound", $"The note with Id = '{id}' was not found");
}
```

Keep errors feature-local unless genuinely shared. `CustomResults.Problem` maps
`Validation`/`Problem` to 400, `NotFound` to 404, `Conflict` to 409, and other
failures to 500. Authentication/permission 401/403 responses come from middleware;
do not assume an error named “Unauthorized” automatically maps to HTTP 403.

### Localize server-generated text

For email, PDF, or notification text, follow [Server Localization](server-localization.md)
for feature-owned markers/resources, culture selection, fallback, and verification.
Client UI strings remain separate; existing API errors are not translated.

## 3. Declare permissions and discovery metadata

**`Features/Notes/NotesPermissions.cs`**

```csharp
namespace AspNetVue.Server.Features.Notes;

internal static class NotesPermissions
{
    internal const string NotesCreate = "notes:create";
    internal const string NotesRead = "notes:read";
}
```

**`Features/Notes/NotesPermissionCatalog.cs`**

```csharp
using AspNetVue.Server.Authorization;

namespace AspNetVue.Server.Features.Notes;

internal sealed class NotesPermissionCatalog : IPermissionCatalog
{
    public IReadOnlyCollection<PermissionDefinition> Permissions { get; } =
    [
        new(NotesPermissions.NotesCreate, []),
        new(NotesPermissions.NotesRead, [])
    ];
}
```

An empty default-role list avoids implicitly granting the feature to ordinary
users; the existing seeder still grants catalog permissions to the admin role.
Add `RoleNames.User` only when the access requirements call for it. Seeded grants
are additive: deleting a definition or removing a default role does not revoke
existing grants. Invalidate affected `IPermissionProvider` entries after changing
persisted role membership/grants.

See [Authentication and Authorization](authentication-authorization.md) for policy
resolution, cache behavior, and startup initialization semantics.

Add `public const string Notes = "Notes";` to `Common/Endpoints/Tags.cs`. This is
HTTP documentation metadata, not a reverse dependency on feature types.

No per-feature DI registrations are required for handlers, validators, endpoints,
EF configurations, or permission catalogs in the server assembly. New integration
services or a new API version may require composition-root changes.

## 4. Implement a command

Each code block below is a separate file in `Features/Notes/CreateNote/`.

**`CreateNoteRequest.cs`**

```csharp
namespace AspNetVue.Server.Features.Notes.CreateNote;

public sealed record CreateNoteRequest(string Title);
```

**`CreateNoteCommand.cs`**

```csharp
using AspNetVue.Server.Common.Messaging;

namespace AspNetVue.Server.Features.Notes.CreateNote;

public sealed record CreateNoteCommand(string Title) : ICommand<Guid>;
```

**`CreateNoteValidator.cs`**

```csharp
using FluentValidation;

namespace AspNetVue.Server.Features.Notes.CreateNote;

public sealed class CreateNoteValidator : AbstractValidator<CreateNoteCommand>
{
    public CreateNoteValidator()
    {
        RuleFor(command => command.Title).NotEmpty().MaximumLength(200);
    }
}
```

**`CreateNoteHandler.cs`**

```csharp
using AspNetVue.Server.Authentication;
using AspNetVue.Server.Common.Results;
using AspNetVue.Server.Common.Messaging;
using AspNetVue.Server.Database;

namespace AspNetVue.Server.Features.Notes.CreateNote;

internal sealed class CreateNoteHandler(
    ApplicationDbContext context,
    IUserContext userContext) : ICommandHandler<CreateNoteCommand, Guid>
{
    public async Task<Result<Guid>> Handle(
        CreateNoteCommand command, CancellationToken cancellationToken)
    {
        var note = new Note
        {
            Id = Guid.NewGuid(),
            UserId = userContext.UserId,
            Title = command.Title
        };

        context.Set<Note>().Add(note);
        await context.SaveChangesAsync(cancellationToken);

        return note.Id;
    }
}
```

The caller ID comes from `IUserContext`, not the request body. For workflows that
accept a target user ID, implement the required ownership/delegation checks and
shared-user existence check explicitly. Inject `IDateTimeProvider` only for business
timestamps (for example `CompletedAt`); create/update/delete audit stamps come from
`BaseEntity` and are written automatically, so handlers must not set them.

**`CreateNoteEndpoint.cs`**

```csharp
using AspNetVue.Server.Common.Endpoints;
using AspNetVue.Server.Common.Messaging;
using AspNetVue.Server.Common.Results;

namespace AspNetVue.Server.Features.Notes.CreateNote;

public sealed class CreateNoteEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("notes", async (
            CreateNoteRequest request,
            ICommandHandler<CreateNoteCommand, Guid> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new CreateNoteCommand(request.Title);
            Result<Guid> result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Notes)
        .MapToApiVersion(1)
        .Produces<Guid>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .HasPermission(NotesPermissions.NotesCreate);
    }
}
```

The resulting route is `/api/v1/notes`; do not include `api/v1` in the relative
route again. Resolve the handler **interface** to get its decorators. The command
chain is `Logging -> Validation -> Handler`; directly constructing a handler in a
test bypasses validation. Returning 200 with the ID matches existing create
endpoints; a different success contract must be declared and tested deliberately.

For commands without a payload, use `ICommand`, `ICommandHandler<TCommand>`,
`Task<Result>`, and `Result.Success()`. At the endpoint, map success with
`result.Match(Results.NoContent, CustomResults.Problem)` and declare HTTP 204.

## 5. Implement a query with record ownership

Each code block below is a separate file in `Features/Notes/GetNoteById/`.

**`GetNoteByIdQuery.cs`**

```csharp
using AspNetVue.Server.Common.Messaging;

namespace AspNetVue.Server.Features.Notes.GetNoteById;

public sealed record GetNoteByIdQuery(Guid NoteId) : IQuery<GetNoteByIdResponse>;
```

**`GetNoteByIdResponse.cs`**

```csharp
namespace AspNetVue.Server.Features.Notes.GetNoteById;

public sealed record GetNoteByIdResponse(Guid Id, string Title);
```

**`GetNoteByIdHandler.cs`**

```csharp
using Microsoft.EntityFrameworkCore;
using AspNetVue.Server.Authentication;
using AspNetVue.Server.Common.Results;
using AspNetVue.Server.Common.Messaging;
using AspNetVue.Server.Database;

namespace AspNetVue.Server.Features.Notes.GetNoteById;

internal sealed class GetNoteByIdHandler(
    ApplicationDbContext context,
    IUserContext userContext) : IQueryHandler<GetNoteByIdQuery, GetNoteByIdResponse>
{
    public async Task<Result<GetNoteByIdResponse>> Handle(
        GetNoteByIdQuery query, CancellationToken cancellationToken)
    {
        Guid userId = userContext.UserId;
        GetNoteByIdResponse? note = await context.Set<Note>()
            .AsNoTracking()
            .Where(note => note.Id == query.NoteId && note.UserId == userId)
            .Select(note => new GetNoteByIdResponse(note.Id, note.Title))
            .SingleOrDefaultAsync(cancellationToken);

        if (note is null)
        {
            return Result.Failure<GetNoteByIdResponse>(NotesErrors.NotFound(query.NoteId));
        }

        return note;
    }
}
```

Filter by the caller when loading the record; a read permission alone is not a
record-ownership check. Returning the same 404 for absent and inaccessible records
avoids revealing another user's record. Apply the same scoped lookup before
updates/deletes. Query validators are not automatically decorated: if query input
needs validation, implement it explicitly rather than adding an unused validator.

**`GetNoteByIdEndpoint.cs`**

```csharp
using AspNetVue.Server.Common.Endpoints;
using AspNetVue.Server.Common.Messaging;
using AspNetVue.Server.Common.Results;

namespace AspNetVue.Server.Features.Notes.GetNoteById;

public sealed class GetNoteByIdEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("notes/{id:guid}", async (
            Guid id,
            IQueryHandler<GetNoteByIdQuery, GetNoteByIdResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetNoteByIdQuery(id);
            Result<GetNoteByIdResponse> result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Notes)
        .MapToApiVersion(1)
        .Produces<GetNoteByIdResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .HasPermission(NotesPermissions.NotesRead);
    }
}
```

## 6. Add optional mechanisms only when needed

| Mechanism | Required decisions |
| --- | --- |
| Caching | Inject `HybridCache`; own keys/tags in the feature. Include caller/resource scope where results are user-specific. Invalidate relevant entries after a successful mutation; never let a cache key bypass record ownership. |
| Domain events | Define the event in its owning domain and implement `IDomainEventHandler<TEvent>` where its reaction belongs. Raise through `Entity.Raise` before saving. Dispatch is post-save, in-process, and not durable/atomic with external effects. Do not import another feature's event directly. |
| Idempotency | Add `.WithIdempotency()` only deliberately and declare/test 409. It rejects repeated keys using cache; it does not replay responses, guarantee exactly-once execution, or remove the prewritten marker when the operation fails. |
| Email/external services | Inject the relevant integration contract, e.g. `IEmailSender`. Provider details belong in `Integrations/`, not endpoints. New implementations require composition-root registration. |
| Authentication only | Use `.RequireAuthorization()` where any authenticated caller may access the operation; do not invent an unnecessary permission. Anonymous access must be an explicit contract decision. |

## 7. Schema and registration checklist

- [ ] Feature entities/configurations stay in the feature; shared identity remains
  under `Database/Entities/Identity/`. No reverse infrastructure-to-feature import.
- [ ] New domain added to the explicit isolation list in
  `tests/AspNetVue.Server.ArchitectureTests/Layers/LayerTests.cs`.
- [ ] Permission constants, catalog defaults, endpoint metadata, and tag added.
- [ ] Database change has a reviewed EF migration and updated model snapshot.
  With a compatible `dotnet-ef` tool available, generate with
  `dotnet ef migrations add <MigrationName> --project src/AspNetVue.Server --output-dir Database/Migrations`.
  Add `// <auto-generated />` to the generated migration `.cs` to avoid the
  enforced file-scoped-namespace error. Review for unintended drops/renames;
  do not apply destructive migrations without authorization.
- [ ] API/OpenAPI documentation, integration tests, and client contracts updated
  when the public API changes. README changes only for template usage/setup;
  architecture documentation changes only for boundaries or execution guarantees.

## 8. Test and verify

Put handler/validator tests under
`tests/AspNetVue.Server.UnitTests/{Domain}/`. Reuse `BaseHandlerTest.CreateDbContext()`
and `CreateCache()` where needed; substitute `IUserContext`, `IDateTimeProvider`, and
external integration contracts. Use a unique database per test. Direct handler
tests do not exercise decorators, middleware, relational constraints, or permission
seeding.

| Scope | Cases to cover |
| --- | --- |
| Command validator | Missing/invalid inputs, boundaries, and valid input. Test the validator directly; do not expect a directly constructed handler to run it. |
| Command handler | Successful persistence; state-dependent failures; caller scoping; cache invalidation/events only when used. |
| Query handler | Owner receives the response; unknown and other-user records do not disclose data. |
| HTTP integration | Success shape/status; command validation -> 400; unauthenticated -> 401; missing permission -> 403; inaccessible resource -> 404 for this recipe; route/version and OpenAPI metadata. |
| PostgreSQL integration | Foreign keys, uniqueness, mappings, and migration behavior; InMemory does not verify these. Seed an actual user for the example's foreign key. |
| Architecture | Sealed handlers/endpoints and no cross-feature or reverse infrastructure dependencies. |

Integration fixtures override JWT settings and rate limits for tests and use their
own PostgreSQL container. Grant test users the required permissions deliberately;
do not bypass real authorization to make a protected endpoint pass.

```bash
dotnet build AspNetVue.slnx
dotnet test tests/AspNetVue.Server.UnitTests --filter 'FullyQualifiedName~Notes'
dotnet test tests/AspNetVue.Server.ArchitectureTests
dotnet test tests/AspNetVue.Server.IntegrationTests --filter 'FullyQualifiedName~Notes'
```

Replace `Notes` with the new tests' namespace/class fragment; a filter matching no
tests is not verification. Integration tests require Docker and `postgres:17`;
Compose PostgreSQL is not their fixture. Run the full suite when the change touches
shared behavior. If client code changes, also run its independent type-check/build.
