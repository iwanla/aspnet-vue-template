using Microsoft.EntityFrameworkCore;
using AspNetVue.Server.Authorization;
using AspNetVue.Server.Authentication;
using AspNetVue.Server.Common.Results;
using AspNetVue.Server.Common.Errors;
using AspNetVue.Server.Common.Messaging;
using AspNetVue.Server.Database;

namespace AspNetVue.Server.Features.Identity.Me;

internal sealed class MeHandler(
    ApplicationDbContext dbContext,
    IUserContext userContext,
    IPermissionProvider permissionProvider)
    : IQueryHandler<MeQuery, MeResponse>
{
    public async Task<Result<MeResponse>> Handle(MeQuery query, CancellationToken cancellationToken)
    {
        Guid userId = userContext.UserId;

        MeResponse? user = await dbContext.Users
            .Where(u => u.Id == userId)
            .Select(u => new MeResponse
            {
                Id = u.Id,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Email = u.Email
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (user is null)
        {
            return Result.Failure<MeResponse>(UserErrors.NotFound(userId));
        }

        IReadOnlySet<string> permissions = await permissionProvider.GetForUserIdAsync(userId, cancellationToken);

        // Sorted so the payload is stable across calls (the provider hands out a set).
        return user with { Permissions = [.. permissions.Order(StringComparer.Ordinal)] };
    }
}
