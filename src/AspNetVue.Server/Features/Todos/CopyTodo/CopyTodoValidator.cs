using FluentValidation;

namespace AspNetVue.Server.Features.Todos;

public sealed class CopyTodoValidator : AbstractValidator<CopyTodoCommand>
{
    public CopyTodoValidator()
    {
        RuleFor(c => c.UserId).NotEmpty();
        RuleFor(c => c.TodoId).NotEmpty();
    }
}
