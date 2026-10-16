using FluentValidation;

namespace AspNetVue.Server.Features.Todos;

public sealed class DeleteTodoValidator : AbstractValidator<DeleteTodoCommand>
{
    public DeleteTodoValidator()
    {
        RuleFor(c => c.TodoItemId).NotEmpty();
    }
}
