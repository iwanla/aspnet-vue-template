using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using AspNetVue.Server.Common.Localization;
using AspNetVue.Server.Features.Identity;
using AspNetVue.Server.Features.Identity.Register;
using AspNetVue.Server.Integrations.Email;
using AspNetVue.Server.UnitTests.Common.Localization;

namespace AspNetVue.Server.UnitTests.Identity;

public sealed class UserRegisteredDomainEventHandlerTests
{
    [Theory]
    [InlineData("en", "Welcome!", "Thanks for registering. We're glad to have you on board.")]
    [InlineData("id", "Selamat datang!", "Terima kasih telah mendaftar. Kami senang Anda bergabung.")]
    public async Task Handle_Should_SendLocalizedWelcomeEmail(string culture, string subject, string body)
    {
        using var scope = new CultureScope(culture);
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddServerLocalization();
        using ServiceProvider provider = services.BuildServiceProvider();
        IStringLocalizer<IdentityResources> localizer =
            provider.GetRequiredService<IStringLocalizer<IdentityResources>>();
        IEmailSender emailSender = Substitute.For<IEmailSender>();
        var handler = new UserRegisteredDomainEventHandler(emailSender, localizer);
        var domainEvent = new UserRegisteredDomainEvent(Guid.NewGuid(), "recipient@example.com");

        await handler.Handle(domainEvent, CancellationToken.None);

        await emailSender.Received(1).SendAsync(domainEvent.Email, subject, body, CancellationToken.None);
    }
}
