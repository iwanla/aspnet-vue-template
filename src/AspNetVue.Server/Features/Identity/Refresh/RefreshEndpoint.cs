using AspNetVue.Server.Common.Results;
using AspNetVue.Server.Common.RateLimiting;
using AspNetVue.Server.Common.Endpoints;
using AspNetVue.Server.Common.Messaging;

namespace AspNetVue.Server.Features.Identity.Refresh;

public sealed class RefreshEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("identity/refresh", async (
            RefreshRequest request,
            ICommandHandler<RefreshCommand, AccessTokensResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new RefreshCommand(request.RefreshToken);

            Result<AccessTokensResponse> result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Identity)
        .MapToApiVersion(1)
        .Produces<AccessTokensResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .Produces(StatusCodes.Status429TooManyRequests)
        .RequireRateLimiting(RateLimitingPolicies.Authentication);
    }
}
