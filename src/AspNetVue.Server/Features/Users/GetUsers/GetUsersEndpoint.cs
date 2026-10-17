using AspNetVue.Server.Common;
using AspNetVue.Server.Common.Endpoints;
using AspNetVue.Server.Common.Extensions;
using AspNetVue.Server.Common.Messaging;

namespace AspNetVue.Server.Features.Users.GetUsers;

public sealed class GetUsersEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("users", async (
            string? search,
            string? sortColumn,
            string? sortOrder,
            int? page,
            int? pageSize,
            IQueryHandler<GetUsersQuery, PagedList<GetUsersResponse>> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetUsersQuery(
                search,
                sortColumn,
                sortOrder,
                page ?? 1,
                pageSize ?? 10);

            Result<PagedList<GetUsersResponse>> result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .HasPermission(UsersPermissions.UsersAccess)
        .WithTags(Tags.Users)
        .MapToApiVersion(1)
        .Produces<PagedList<GetUsersResponse>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);
    }
}
