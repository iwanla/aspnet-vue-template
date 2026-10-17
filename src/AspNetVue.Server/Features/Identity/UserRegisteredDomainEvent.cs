using AspNetVue.Server.Common;

namespace AspNetVue.Server.Features.Identity;

public sealed record UserRegisteredDomainEvent(Guid UserId, string Email) : IDomainEvent;
