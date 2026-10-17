using AspNetVue.Server.Common.Messaging;

namespace AspNetVue.Server.Features.Users.GetUserByEmail;

public sealed record GetUserByEmailQuery(string Email) : IQuery<GetUserByEmailResponse>;
