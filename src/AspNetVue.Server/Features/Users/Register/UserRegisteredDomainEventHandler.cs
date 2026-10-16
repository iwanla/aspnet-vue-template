using AspNetVue.Server.Common;
using AspNetVue.Server.Features.Users;
using AspNetVue.Server.Integrations.Email;

namespace AspNetVue.Server.Features.Users.Register;

internal sealed class UserRegisteredDomainEventHandler(IEmailSender emailSender)
    : IDomainEventHandler<UserRegisteredDomainEvent>
{
    public async Task Handle(UserRegisteredDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        await emailSender.SendAsync(
            domainEvent.Email,
            "Welcome!",
            "Thanks for registering. We're glad to have you on board.",
            cancellationToken);
    }
}
