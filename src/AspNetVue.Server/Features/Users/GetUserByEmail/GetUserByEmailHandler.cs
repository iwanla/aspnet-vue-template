using Microsoft.EntityFrameworkCore;
using AspNetVue.Server.Authentication;
using AspNetVue.Server.Common;
using AspNetVue.Server.Common.Errors;
using AspNetVue.Server.Common.Messaging;
using AspNetVue.Server.Database;

namespace AspNetVue.Server.Features.Users.GetUserByEmail;

internal sealed class GetUserByEmailHandler(ApplicationDbContext context, IUserContext userContext)
    : IQueryHandler<GetUserByEmailQuery, GetUserByEmailResponse>
{
    public async Task<Result<GetUserByEmailResponse>> Handle(GetUserByEmailQuery query, CancellationToken cancellationToken)
    {
        GetUserByEmailResponse? user = await context.Users
            .Where(u => u.Email == query.Email)
            .Select(u => new GetUserByEmailResponse
            {
                Id = u.Id,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Email = u.Email
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (user is null)
        {
            return Result.Failure<GetUserByEmailResponse>(UserErrors.NotFoundByEmail);
        }

        if (user.Id != userContext.UserId)
        {
            return Result.Failure<GetUserByEmailResponse>(UserErrors.Unauthorized());
        }

        return user;
    }
}
