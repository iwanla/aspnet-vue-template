using Microsoft.EntityFrameworkCore;
using AspNetVue.Server.Authentication;
using AspNetVue.Server.Common;
using AspNetVue.Server.Common.Errors;
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
        IDateTimeProvider dateTimeProvider = Substitute.For<IDateTimeProvider>();

        var handler = new CreateTodoHandler(context, dateTimeProvider, userContext);

        Result<Guid> result = await handler.Handle(Command, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(UserErrors.Unauthorized());
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenUserDoesNotExist()
    {
        await using ApplicationDbContext context = CreateDbContext();
        IUserContext userContext = Substitute.For<IUserContext>();
        userContext.UserId.Returns(UserId);
        IDateTimeProvider dateTimeProvider = Substitute.For<IDateTimeProvider>();

        var handler = new CreateTodoHandler(context, dateTimeProvider, userContext);

        Result<Guid> result = await handler.Handle(Command, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(UserErrors.NotFound(UserId));
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
        IDateTimeProvider dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(DateTime.UtcNow);

        var handler = new CreateTodoHandler(context, dateTimeProvider, userContext);

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
