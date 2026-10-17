using AspNetVue.Server.Database.Entities.Identity;

namespace AspNetVue.Server.Authentication;

public interface ITokenProvider
{
    string Create(User user);

    string GenerateRefreshToken();
}
