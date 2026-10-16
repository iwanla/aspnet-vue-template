using AspNetVue.Server.Common.Messaging;

namespace AspNetVue.Server.Features.Users.Register;

public sealed record RegisterCommand(string Email, string FirstName, string LastName, string Password)
    : ICommand<Guid>;
