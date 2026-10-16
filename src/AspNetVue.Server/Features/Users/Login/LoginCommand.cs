using AspNetVue.Server.Common.Messaging;

namespace AspNetVue.Server.Features.Users;

public sealed record LoginCommand(string Email, string Password) : ICommand<AccessTokensResponse>;
