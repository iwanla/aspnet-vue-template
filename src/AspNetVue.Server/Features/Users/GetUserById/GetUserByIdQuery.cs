using AspNetVue.Server.Common.Messaging;

namespace AspNetVue.Server.Features.Users.GetUserById;

public sealed record GetUserByIdQuery(Guid UserId) : IQuery<GetUserByIdResponse>;
