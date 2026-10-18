using AspNetVue.Server.Common.Results;
using AspNetVue.Server.Common.Messaging;

namespace AspNetVue.Server.Features.Todos.GetTodos;

public sealed record GetTodosQuery(
    Guid UserId,
    string? Search,
    string? SortColumn,
    string? SortOrder,
    int Page,
    int PageSize) : IQuery<PagedList<GetTodosResponse>>;
