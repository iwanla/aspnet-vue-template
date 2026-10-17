using AspNetVue.Server.Common.Messaging;

namespace AspNetVue.Server.Features.Users.Refresh;

public sealed record RefreshCommand(string RefreshToken) : ICommand<AccessTokensResponse>;
