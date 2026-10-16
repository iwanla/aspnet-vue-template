using AspNetVue.Server.Features.Users;

namespace AspNetVue.Server.Authentication;

public interface ITokenProvider
{
    string Create(User user);

    string GenerateRefreshToken();
}
