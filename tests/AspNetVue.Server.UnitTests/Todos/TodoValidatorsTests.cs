using FluentValidation.TestHelper;
using AspNetVue.Server.Features.Todos;
using AspNetVue.Server.Features.Todos.CreateTodo;
using AspNetVue.Server.Features.Todos.UpdateTodo;

namespace AspNetVue.Server.UnitTests.Todos;

public sealed class TodoValidatorsTests
{
    private readonly CreateTodoValidator _createValidator = new();
    private readonly UpdateTodoValidator _updateValidator = new();

    [Fact]
    public void CreateValidator_Should_HaveError_WhenDescriptionIsEmpty()
    {
        var command = new CreateTodoCommand
        {
            UserId = Guid.NewGuid(),
            Description = string.Empty,
            Priority = Priority.Low
        };

        TestValidationResult<CreateTodoCommand> result = _createValidator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Description);
    }

    [Fact]
    public void CreateValidator_Should_NotHaveErrors_WhenCommandIsValid()
    {
        var command = new CreateTodoCommand
        {
            UserId = Guid.NewGuid(),
            Description = "Buy groceries",
            Priority = Priority.Medium,
            Labels = ["home"]
        };

        TestValidationResult<CreateTodoCommand> result = _createValidator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void UpdateValidator_Should_HaveError_WhenDescriptionExceedsMaxLength()
    {
        var command = new UpdateTodoCommand(Guid.NewGuid(), new string('a', 501));

        TestValidationResult<UpdateTodoCommand> result = _updateValidator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Description);
    }

    [Fact]
    public void UpdateValidator_Should_NotHaveErrors_WhenCommandIsValid()
    {
        var command = new UpdateTodoCommand(Guid.NewGuid(), "Updated description");

        TestValidationResult<UpdateTodoCommand> result = _updateValidator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
