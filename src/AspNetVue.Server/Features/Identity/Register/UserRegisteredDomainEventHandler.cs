using Microsoft.Extensions.Localization;
using AspNetVue.Server.Common.Domain;
using AspNetVue.Server.Features.Identity;
using AspNetVue.Server.Integrations.Email;

namespace AspNetVue.Server.Features.Identity.Register;

internal sealed class UserRegisteredDomainEventHandler(
    IEmailSender emailSender,
    IStringLocalizer<UserRegisteredDomainEventHandler> localizer)
    : IDomainEventHandler<UserRegisteredDomainEvent>
{
    public async Task Handle(UserRegisteredDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        await emailSender.SendAsync(
            domainEvent.Email,
            localizer["identity.registration.welcomeSubject"],
            localizer["identity.registration.welcomeBody"],
            cancellationToken);
    }
}
