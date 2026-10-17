using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using AspNetVue.Server.Authentication;
using AspNetVue.Server.Common;
using AspNetVue.Server.Common.Messaging;
using AspNetVue.Server.Database;

namespace AspNetVue.Server.Features.Todos.GetTodoById;

internal sealed class GetTodoByIdHandler(
    ApplicationDbContext context,
    IUserContext userContext,
    HybridCache cache)
    : IQueryHandler<GetTodoByIdQuery, GetTodoByIdResponse>
{
    public async Task<Result<GetTodoByIdResponse>> Handle(GetTodoByIdQuery query, CancellationToken cancellationToken)
    {
        Guid userId = userContext.UserId;

        GetTodoByIdResponse? todo = await cache.GetOrCreateAsync(
            TodoCacheKeys.ById(userId, query.TodoItemId),
            async cancellation => await context.TodoItems
                .Where(todoItem => todoItem.Id == query.TodoItemId && todoItem.UserId == userId)
                .Select(todoItem => new GetTodoByIdResponse
                {
                    Id = todoItem.Id,
                    UserId = todoItem.UserId,
                    Description = todoItem.Description,
                    DueDate = todoItem.DueDate,
                    Labels = todoItem.Labels,
                    IsCompleted = todoItem.IsCompleted,
                    CreatedAt = todoItem.CreatedAt,
                    CompletedAt = todoItem.CompletedAt
                })
                .SingleOrDefaultAsync(cancellation),
            cancellationToken: cancellationToken);

        if (todo is null)
        {
            return Result.Failure<GetTodoByIdResponse>(TodoItemErrors.NotFound(query.TodoItemId));
        }

        return todo;
    }
}
