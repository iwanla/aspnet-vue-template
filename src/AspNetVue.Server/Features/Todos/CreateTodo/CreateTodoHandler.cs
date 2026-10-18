using Microsoft.EntityFrameworkCore;
using AspNetVue.Server.Authentication;
using AspNetVue.Server.Common.Results;
using AspNetVue.Server.Common.Messaging;
using AspNetVue.Server.Database;
using AspNetVue.Server.Database.Entities.Identity;

namespace AspNetVue.Server.Features.Todos.CreateTodo;

internal sealed class CreateTodoHandler(
    ApplicationDbContext context,
    IUserContext userContext) : ICommandHandler<CreateTodoCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateTodoCommand command, CancellationToken cancellationToken)
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

        var todoItem = new TodoItem
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Description = command.Description,
            Priority = command.Priority,
            DueDate = command.DueDate,
            Labels = command.Labels,
            IsCompleted = false
        };

        todoItem.Raise(new TodoItemCreatedDomainEvent(todoItem.Id));

        context.Set<TodoItem>().Add(todoItem);

        await context.SaveChangesAsync(cancellationToken);

        return todoItem.Id;
    }
}
