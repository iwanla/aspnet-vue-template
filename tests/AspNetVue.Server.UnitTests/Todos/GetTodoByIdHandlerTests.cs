using Microsoft.Extensions.Caching.Hybrid;
using AspNetVue.Server.Authentication;
using AspNetVue.Server.Common.Results;
using AspNetVue.Server.Database;
using AspNetVue.Server.Features.Todos;
using AspNetVue.Server.Features.Todos.GetTodoById;
using AspNetVue.Server.UnitTests.Abstractions;

namespace AspNetVue.Server.UnitTests.Todos;

public sealed class GetTodoByIdHandlerTests : BaseHandlerTest
{
    private static readonly Guid UserId = Guid.NewGuid();

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenTodoDoesNotExist()
    {
        // Arrange
        await using ApplicationDbContext context = CreateDbContext();
        HybridCache cache = CreateCache();
        IUserContext userContext = Substitute.For<IUserContext>();
        userContext.UserId.Returns(UserId);

        var handler = new GetTodoByIdHandler(context, userContext, cache);
        var query = new GetTodoByIdQuery(Guid.NewGuid());

        // Act
        Result<GetTodoByIdResponse> result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("TodoItems.NotFound");
    }

    [Fact]
    public async Task Handle_Should_ReturnTodo_WhenItExistsForTheCurrentUser()
    {
        // Arrange
        await using ApplicationDbContext context = CreateDbContext();
        var todoItem = new TodoItem
        {
            Id = Guid.NewGuid(),
            UserId = UserId,
            Description = "Cached todo",
            Priority = Priority.High,
            Labels = ["important"],
            IsCompleted = false,
            CreatedAt = DateTime.UtcNow
        };
        context.Set<TodoItem>().Add(todoItem);
        await context.SaveChangesAsync();

        HybridCache cache = CreateCache();
        IUserContext userContext = Substitute.For<IUserContext>();
        userContext.UserId.Returns(UserId);

        var handler = new GetTodoByIdHandler(context, userContext, cache);
        var query = new GetTodoByIdQuery(todoItem.Id);

        // Act
        Result<GetTodoByIdResponse> result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Id.ShouldBe(todoItem.Id);
        result.Value.Description.ShouldBe("Cached todo");
    }
}
