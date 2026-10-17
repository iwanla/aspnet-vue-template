using AspNetVue.Server.Common.Messaging;

namespace AspNetVue.Server.Features.Identity.Refresh;

public sealed record RefreshCommand(string RefreshToken) : ICommand<AccessTokensResponse>;
