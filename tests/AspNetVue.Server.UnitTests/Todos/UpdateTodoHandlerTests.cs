using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using AspNetVue.Server.Authentication;
using AspNetVue.Server.Common;
using AspNetVue.Server.Database;
using AspNetVue.Server.Features.Todos;
using AspNetVue.Server.Features.Todos.UpdateTodo;
using AspNetVue.Server.UnitTests.Abstractions;

namespace AspNetVue.Server.UnitTests.Todos;

public sealed class UpdateTodoHandlerTests : BaseHandlerTest
{
    private static readonly Guid UserId = Guid.NewGuid();

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenTodoBelongsToAnotherUser()
    {
        // Arrange
        await using ApplicationDbContext context = CreateDbContext();
        Guid todoItemId = await SeedTodoAsync(context, ownerId: Guid.NewGuid());

        HybridCache cache = CreateCache();
        IUserContext userContext = Substitute.For<IUserContext>();
        userContext.UserId.Returns(UserId);

        var handler = new UpdateTodoHandler(context, userContext, cache);
        var command = new UpdateTodoCommand(todoItemId, "Updated description");

        // Act
        Result result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("TodoItems.NotFound");
    }

    [Fact]
    public async Task Handle_Should_UpdateDescriptionAndDispatchDomainEvent_WhenValid()
    {
        // Arrange
        IDomainEventsDispatcher dispatcher = Substitute.For<IDomainEventsDispatcher>();
        await using ApplicationDbContext context = CreateDbContext(dispatcher);
        Guid todoItemId = await SeedTodoAsync(context, ownerId: UserId);

        HybridCache cache = CreateCache();
        IUserContext userContext = Substitute.For<IUserContext>();
        userContext.UserId.Returns(UserId);

        var handler = new UpdateTodoHandler(context, userContext, cache);
        var command = new UpdateTodoCommand(todoItemId, "Updated description");

        // Act
        Result result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();

        TodoItem todoItem = await context.Set<TodoItem>().SingleAsync(t => t.Id == todoItemId);
        todoItem.Description.ShouldBe("Updated description");
        await dispatcher.Received().DispatchAsync(
            Arg.Is<IEnumerable<IDomainEvent>>(events => events.Any(e => e is TodoItemUpdatedDomainEvent)),
            Arg.Any<CancellationToken>());
    }

    private static async Task<Guid> SeedTodoAsync(ApplicationDbContext context, Guid ownerId)
    {
        var todoItem = new TodoItem
        {
            Id = Guid.NewGuid(),
            UserId = ownerId,
            Description = "Original description",
            Priority = Priority.Low,
            Labels = [],
            IsCompleted = false,
            CreatedAt = DateTime.UtcNow
        };

        context.Set<TodoItem>().Add(todoItem);
        await context.SaveChangesAsync();

        return todoItem.Id;
    }
}
