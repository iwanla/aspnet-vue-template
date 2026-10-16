using AspNetVue.Server.Common;
using AspNetVue.Server.Common.Messaging;

namespace AspNetVue.Server.Features.Todos;

public sealed record GetTodosQuery(
    Guid UserId,
    string? Search,
    string? SortColumn,
    string? SortOrder,
    int Page,
    int PageSize) : IQuery<PagedList<GetTodosResponse>>;
