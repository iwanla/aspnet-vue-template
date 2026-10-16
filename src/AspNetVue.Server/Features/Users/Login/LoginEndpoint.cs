using AspNetVue.Server.Common;
using AspNetVue.Server.Common.Endpoints;
using AspNetVue.Server.Common.Extensions;
using AspNetVue.Server.Common.Messaging;

namespace AspNetVue.Server.Features.Users;

public sealed class LoginEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("users/login", async (
            LoginRequest request,
            ICommandHandler<LoginCommand, AccessTokensResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new LoginCommand(request.Email, request.Password);

            Result<AccessTokensResponse> result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Users)
        .RequireRateLimiting(RateLimitingPolicies.Authentication);
    }
}
