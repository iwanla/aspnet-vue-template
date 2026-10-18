using Microsoft.EntityFrameworkCore;
using AspNetVue.Server.Authentication;
using AspNetVue.Server.Common.Results;
using AspNetVue.Server.Common.Messaging;
using AspNetVue.Server.Database;

namespace AspNetVue.Server.Features.Users.GetUserById;

internal sealed class GetUserByIdHandler(ApplicationDbContext context, IUserContext userContext)
    : IQueryHandler<GetUserByIdQuery, GetUserByIdResponse>
{
    public async Task<Result<GetUserByIdResponse>> Handle(GetUserByIdQuery query, CancellationToken cancellationToken)
    {
        if (query.UserId != userContext.UserId)
        {
            return Result.Failure<GetUserByIdResponse>(UserErrors.Unauthorized());
        }

        GetUserByIdResponse? user = await context.Users
            .Where(u => u.Id == query.UserId)
            .Select(u => new GetUserByIdResponse
            {
                Id = u.Id,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Email = u.Email
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (user is null)
        {
            return Result.Failure<GetUserByIdResponse>(UserErrors.NotFound(query.UserId));
        }

        return user;
    }
}
