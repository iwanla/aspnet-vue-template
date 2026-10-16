using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using AspNetVue.Server.Authentication;
using AspNetVue.Server.Common;
using AspNetVue.Server.Common.Messaging;
using AspNetVue.Server.Database;

namespace AspNetVue.Server.Features.Todos;

internal sealed class UpdateTodoHandler(
    ApplicationDbContext context,
    IUserContext userContext,
    HybridCache cache)
    : ICommandHandler<UpdateTodoCommand>
{
    public async Task<Result> Handle(UpdateTodoCommand command, CancellationToken cancellationToken)
    {
        TodoItem? todoItem = await context.TodoItems
            .SingleOrDefaultAsync(
                t => t.Id == command.TodoItemId && t.UserId == userContext.UserId,
                cancellationToken);

        if (todoItem is null)
        {
            return Result.Failure(TodoItemErrors.NotFound(command.TodoItemId));
        }

        todoItem.Description = command.Description;

        todoItem.Raise(new TodoItemUpdatedDomainEvent(todoItem.Id));

        await context.SaveChangesAsync(cancellationToken);

        await cache.RemoveAsync(TodoCacheKeys.ById(todoItem.UserId, todoItem.Id), cancellationToken);

        return Result.Success();
    }
}
