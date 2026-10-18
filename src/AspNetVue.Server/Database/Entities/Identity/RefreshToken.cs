using AspNetVue.Server.Common.Domain;

namespace AspNetVue.Server.Database.Entities.Identity;

public sealed class RefreshToken : BaseEntity
{
    public string Token { get; set; }
    public Guid UserId { get; set; }
    public DateTime ExpiresOnUtc { get; set; }
    public User User { get; set; }
}
