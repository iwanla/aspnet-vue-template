using AspNetVue.Server.Common.Messaging;

namespace AspNetVue.Server.Features.Users;

public sealed record GetUserByIdQuery(Guid UserId) : IQuery<GetUserByIdResponse>;
