using Microsoft.EntityFrameworkCore;
using AspNetVue.Server.Authentication;
using AspNetVue.Server.Common.Results;
using AspNetVue.Server.Common.Messaging;
using AspNetVue.Server.Database;
using AspNetVue.Server.Database.Entities.Identity;

namespace AspNetVue.Server.Features.Todos.CopyTodo;

internal sealed class CopyTodoHandler(
    ApplicationDbContext context,
    IUserContext userContext)
    : ICommandHandler<CopyTodoCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CopyTodoCommand command, CancellationToken cancellationToken)
    {
        if (userContext.UserId != command.UserId)
        {
            return Result.Failure<Guid>(TodoItemErrors.Unauthorized());
        }

        User? user = await context.Users.AsNoTracking()
            .SingleOrDefaultAsync(u => u.Id == command.UserId, cancellationToken);

        if (user is null)
        {
            return Result.Failure<Guid>(TodoItemErrors.UserNotFound(command.UserId));
        }

        TodoItem? existingTodo = await context.Set<TodoItem>().AsNoTracking()
            .SingleOrDefaultAsync(t => t.Id == command.TodoId && t.UserId == command.UserId, cancellationToken);

        if (existingTodo is null)
        {
            return Result.Failure<Guid>(TodoItemErrors.NotFound(command.TodoId));
        }

        var copiedTodoItem = new TodoItem
        {
            UserId = user.Id,
            Description = existingTodo.Description,
            Priority = existingTodo.Priority,
            DueDate = existingTodo.DueDate,
            Labels = [.. existingTodo.Labels], // Create a new list to avoid reference issues
            IsCompleted = false // Reset completion status for the copy
        };

        copiedTodoItem.Raise(new TodoItemCreatedDomainEvent(copiedTodoItem.Id));

        context.Set<TodoItem>().Add(copiedTodoItem);

        await context.SaveChangesAsync(cancellationToken);

        return copiedTodoItem.Id;
    }
}
