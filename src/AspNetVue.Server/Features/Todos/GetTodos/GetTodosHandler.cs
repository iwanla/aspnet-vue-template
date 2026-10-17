using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using AspNetVue.Server.Authentication;
using AspNetVue.Server.Common;
using AspNetVue.Server.Common.Errors;
using AspNetVue.Server.Common.Messaging;
using AspNetVue.Server.Database;

namespace AspNetVue.Server.Features.Todos.GetTodos;

internal sealed class GetTodosHandler(ApplicationDbContext context, IUserContext userContext)
    : IQueryHandler<GetTodosQuery, PagedList<GetTodosResponse>>
{
    public async Task<Result<PagedList<GetTodosResponse>>> Handle(GetTodosQuery query, CancellationToken cancellationToken)
    {
        if (query.UserId != userContext.UserId)
        {
            return Result.Failure<PagedList<GetTodosResponse>>(UserErrors.Unauthorized());
        }

        IQueryable<TodoItem> todosQuery = context.Set<TodoItem>()
            .Where(todoItem => todoItem.UserId == query.UserId);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            string search = query.Search;
            todosQuery = todosQuery.Where(todoItem => todoItem.Description.Contains(search));
        }

        todosQuery = string.Equals(query.SortOrder, "desc", StringComparison.OrdinalIgnoreCase)
            ? todosQuery.OrderByDescending(GetSortProperty(query.SortColumn))
            : todosQuery.OrderBy(GetSortProperty(query.SortColumn));

        IQueryable<GetTodosResponse> responsesQuery = todosQuery
            .Select(todoItem => new GetTodosResponse
            {
                Id = todoItem.Id,
                UserId = todoItem.UserId,
                Description = todoItem.Description,
                DueDate = todoItem.DueDate,
                Labels = todoItem.Labels,
                IsCompleted = todoItem.IsCompleted,
                CreatedAt = todoItem.CreatedAt,
                CompletedAt = todoItem.CompletedAt
            });

        PagedList<GetTodosResponse> todos = await PagedList<GetTodosResponse>.CreateAsync(
            responsesQuery,
            query.Page,
            query.PageSize,
            cancellationToken);

        return todos;
    }

    private static Expression<Func<TodoItem, object>> GetSortProperty(string? sortColumn) =>
        sortColumn?.ToUpperInvariant() switch
        {
            "DESCRIPTION" => todoItem => todoItem.Description,
            "DUEDATE" => todoItem => todoItem.DueDate!,
            "PRIORITY" => todoItem => todoItem.Priority,
            _ => todoItem => todoItem.CreatedAt
        };
}
