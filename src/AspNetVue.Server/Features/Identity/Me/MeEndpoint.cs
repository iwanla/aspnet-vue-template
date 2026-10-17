using AspNetVue.Server.Common;
using AspNetVue.Server.Common.Endpoints;
using AspNetVue.Server.Common.Extensions;
using AspNetVue.Server.Common.Messaging;

namespace AspNetVue.Server.Features.Identity.Me;

public sealed class MeEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("identity/me", async (
            IQueryHandler<MeQuery, MeResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new MeQuery();

            Result<MeResponse> result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        // Any authenticated user may read their own profile: no permission is required, so the
        // `user` role can render its own session without users:access.
        .RequireAuthorization()
        .WithTags(Tags.Identity)
        .MapToApiVersion(1)
        .Produces<MeResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .Produces(StatusCodes.Status401Unauthorized);
    }
}
