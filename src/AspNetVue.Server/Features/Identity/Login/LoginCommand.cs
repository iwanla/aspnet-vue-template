using AspNetVue.Server.Common.Messaging;

namespace AspNetVue.Server.Features.Identity.Login;

public sealed record LoginCommand(string Email, string Password) : ICommand<AccessTokensResponse>;
