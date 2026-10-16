using FluentValidation;

namespace AspNetVue.Server.Features.Todos;

public sealed class CompleteTodoValidator : AbstractValidator<CompleteTodoCommand>
{
    public CompleteTodoValidator()
    {
        RuleFor(c => c.TodoItemId).NotEmpty();
    }
}
