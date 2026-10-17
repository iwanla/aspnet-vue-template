using AspNetVue.Server.Common.Messaging;

namespace AspNetVue.Server.Features.Identity.Me;

/// <summary>Reads the profile of the caller; the user id comes from <c>IUserContext</c>, never the request.</summary>
public sealed record MeQuery : IQuery<MeResponse>;
