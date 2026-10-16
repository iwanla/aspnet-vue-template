using AspNetVue.Server.Common.Messaging;

namespace AspNetVue.Server.Features.Users;

public sealed record GetUserByEmailQuery(string Email) : IQuery<GetUserByEmailResponse>;
