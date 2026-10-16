using FluentValidation;

namespace AspNetVue.Server.Features.Users;

public sealed class RefreshValidator : AbstractValidator<RefreshCommand>
{
    public RefreshValidator()
    {
        RuleFor(c => c.RefreshToken).NotEmpty();
    }
}
