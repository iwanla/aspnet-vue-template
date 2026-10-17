using Microsoft.EntityFrameworkCore;
using AspNetVue.Server.Authentication;
using AspNetVue.Server.Authorization;
using AspNetVue.Server.Common;
using AspNetVue.Server.Common.Messaging;
using AspNetVue.Server.Database;
using AspNetVue.Server.Database.Entities.Identity;
using AspNetVue.Server.Features.Identity;

namespace AspNetVue.Server.Features.Identity.Register;

internal sealed class RegisterHandler(ApplicationDbContext context, IPasswordHasher passwordHasher)
    : ICommandHandler<RegisterCommand, Guid>
{
    public async Task<Result<Guid>> Handle(RegisterCommand command, CancellationToken cancellationToken)
    {
        if (await context.Users.AnyAsync(u => u.Email == command.Email, cancellationToken))
        {
            return Result.Failure<Guid>(IdentityErrors.EmailNotUnique);
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = command.Email,
            FirstName = command.FirstName,
            LastName = command.LastName,
            PasswordHash = passwordHasher.Hash(command.Password)
        };

        user.Raise(new UserRegisteredDomainEvent(user.Id, user.Email));

        context.Users.Add(user);

        await AssignDefaultRolesAsync(user, cancellationToken);

        await context.SaveChangesAsync(cancellationToken);

        return user.Id;
    }

    /// <summary>
    /// Every new user receives the <c>user</c> role. The first user additionally receives
    /// <c>admin</c> so the template is usable without editing the database by hand. Roles are only
    /// assigned when they exist, which keeps registration working before the seeder has run.
    /// </summary>
    private async Task AssignDefaultRolesAsync(User user, CancellationToken cancellationToken)
    {
        Guid? userRoleId = await FindRoleIdAsync(RoleNames.User, cancellationToken);

        if (userRoleId is not null)
        {
            context.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = userRoleId.Value });
        }

        Guid? adminRoleId = await FindRoleIdAsync(RoleNames.Admin, cancellationToken);

        if (adminRoleId is not null &&
            !await context.UserRoles.AnyAsync(userRole => userRole.RoleId == adminRoleId.Value, cancellationToken))
        {
            context.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = adminRoleId.Value });
        }
    }

    private async Task<Guid?> FindRoleIdAsync(string roleName, CancellationToken cancellationToken) =>
        await context.Roles
            .Where(role => role.Name == roleName)
            .Select(role => (Guid?)role.Id)
            .FirstOrDefaultAsync(cancellationToken);
}
