using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using AspNetVue.Server.Common.Results;
using AspNetVue.Server.Common.Messaging;
using AspNetVue.Server.Database;
using AspNetVue.Server.Database.Entities.Identity;

namespace AspNetVue.Server.Features.Users.GetUsers;

internal sealed class GetUsersHandler(ApplicationDbContext context)
    : IQueryHandler<GetUsersQuery, PagedList<GetUsersResponse>>
{
    public async Task<Result<PagedList<GetUsersResponse>>> Handle(GetUsersQuery query, CancellationToken cancellationToken)
    {
        IQueryable<User> usersQuery = context.Users;

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            string search = query.Search;
            usersQuery = usersQuery.Where(user =>
                user.Email.Contains(search) ||
                user.FirstName.Contains(search) ||
                user.LastName.Contains(search));
        }

        usersQuery = string.Equals(query.SortOrder, "desc", StringComparison.OrdinalIgnoreCase)
            ? usersQuery.OrderByDescending(GetSortProperty(query.SortColumn))
            : usersQuery.OrderBy(GetSortProperty(query.SortColumn));

        IQueryable<GetUsersResponse> responsesQuery = usersQuery
            .Select(user => new GetUsersResponse
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName
            });

        PagedList<GetUsersResponse> users = await PagedList<GetUsersResponse>.CreateAsync(
            responsesQuery,
            query.Page,
            query.PageSize,
            cancellationToken);

        return users;
    }

    private static Expression<Func<User, object>> GetSortProperty(string? sortColumn) =>
        sortColumn?.ToUpperInvariant() switch
        {
            "EMAIL" => user => user.Email,
            "FIRSTNAME" => user => user.FirstName,
            "LASTNAME" => user => user.LastName,
            _ => user => user.Email
        };
}
