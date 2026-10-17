using AspNetVue.Server.Common;
using AspNetVue.Server.Common.Messaging;

namespace AspNetVue.Server.Features.Users.GetUsers;

public sealed record GetUsersQuery(
    string? Search,
    string? SortColumn,
    string? SortOrder,
    int Page,
    int PageSize) : IQuery<PagedList<GetUsersResponse>>;
