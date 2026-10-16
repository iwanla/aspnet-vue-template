using AspNetVue.Server.Common;
using AspNetVue.Server.Common.Endpoints;
using AspNetVue.Server.Common.Extensions;
using AspNetVue.Server.Common.Messaging;

namespace AspNetVue.Server.Features.Users;

public sealed class RefreshEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("users/refresh-token", async (
            RefreshRequest request,
            ICommandHandler<RefreshCommand, AccessTokensResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new RefreshCommand(request.RefreshToken);

            Result<AccessTokensResponse> result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Users)
        .RequireRateLimiting(RateLimitingPolicies.Authentication);
    }
}
