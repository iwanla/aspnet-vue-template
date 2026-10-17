using FluentValidation;

namespace AspNetVue.Server.Features.Todos.DeleteTodo;

public sealed class DeleteTodoValidator : AbstractValidator<DeleteTodoCommand>
{
    public DeleteTodoValidator()
    {
        RuleFor(c => c.TodoItemId).NotEmpty();
    }
}
