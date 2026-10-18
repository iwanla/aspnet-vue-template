using Microsoft.EntityFrameworkCore;
using AspNetVue.Server.Authentication;
using AspNetVue.Server.Common.Domain;
using AspNetVue.Server.Common.Results;
using AspNetVue.Server.Database;
using AspNetVue.Server.Database.Entities.Identity;
using AspNetVue.Server.Features.Todos;
using AspNetVue.Server.Features.Todos.CreateTodo;
using AspNetVue.Server.UnitTests.Abstractions;

namespace AspNetVue.Server.UnitTests.Todos;

public sealed class CreateTodoHandlerTests : BaseHandlerTest
{
    private static readonly Guid UserId = Guid.NewGuid();

    private static CreateTodoCommand Command => new()
    {
        UserId = UserId,
        Description = "Write unit tests",
        Priority = Priority.Medium,
        Labels = ["work"]
    };

    [Fact]
    public async Task Handle_Should_ReturnUnauthorized_WhenUserIdDoesNotMatchContext()
    {
        await using ApplicationDbContext context = CreateDbContext();
        IUserContext userContext = Substitute.For<IUserContext>();
        userContext.UserId.Returns(Guid.NewGuid());

        var handler = new CreateTodoHandler(context, userContext);

        Result<Guid> result = await handler.Handle(Command, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(Error.Failure(
            "Users.Unauthorized", "You are not authorized to perform this action."));
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenUserDoesNotExist()
    {
        await using ApplicationDbContext context = CreateDbContext();
        IUserContext userContext = Substitute.For<IUserContext>();
        userContext.UserId.Returns(UserId);

        var handler = new CreateTodoHandler(context, userContext);

        Result<Guid> result = await handler.Handle(Command, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(Error.NotFound(
            "Users.NotFound", $"The user with the Id = '{UserId}' was not found"));
    }

    [Fact]
    public async Task Handle_Should_PersistTodoAndDispatchDomainEvent_WhenValid()
    {
        IDomainEventsDispatcher dispatcher = Substitute.For<IDomainEventsDispatcher>();
        await using ApplicationDbContext context = CreateDbContext(dispatcher);
        context.Users.Add(new User
        {
            Id = UserId,
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            PasswordHash = "hash"
        });
        await context.SaveChangesAsync();

        IUserContext userContext = Substitute.For<IUserContext>();
        userContext.UserId.Returns(UserId);

        var handler = new CreateTodoHandler(context, userContext);

        Result<Guid> result = await handler.Handle(Command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        TodoItem todoItem = await context.Set<TodoItem>().SingleAsync(t => t.Id == result.Value);
        todoItem.Description.ShouldBe("Write unit tests");
        todoItem.UserId.ShouldBe(UserId);
        await dispatcher.Received().DispatchAsync(
            Arg.Is<IEnumerable<IDomainEvent>>(events => events.Any(e => e is TodoItemCreatedDomainEvent)),
            Arg.Any<CancellationToken>());
    }
}
