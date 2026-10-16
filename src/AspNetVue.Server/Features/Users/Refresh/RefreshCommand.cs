using AspNetVue.Server.Common.Messaging;

namespace AspNetVue.Server.Features.Users;

public sealed record RefreshCommand(string RefreshToken) : ICommand<AccessTokensResponse>;
