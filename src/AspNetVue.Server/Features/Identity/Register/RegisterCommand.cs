using AspNetVue.Server.Common.Messaging;

namespace AspNetVue.Server.Features.Identity.Register;

public sealed record RegisterCommand(string Email, string FirstName, string LastName, string Password)
    : ICommand<Guid>;
