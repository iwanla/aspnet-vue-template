using FluentValidation;

namespace AspNetVue.Server.Features.Todos.UpdateTodo;

public sealed class UpdateTodoValidator : AbstractValidator<UpdateTodoCommand>
{
    public UpdateTodoValidator()
    {
        RuleFor(c => c.TodoItemId).NotEmpty();

        RuleFor(c => c.Description).NotEmpty().MaximumLength(500);
    }
}
