using FluentValidation;

namespace AspNetVue.Server.Features.Users.Refresh;

public sealed class RefreshValidator : AbstractValidator<RefreshCommand>
{
    public RefreshValidator()
    {
        RuleFor(c => c.RefreshToken).NotEmpty();
    }
}
