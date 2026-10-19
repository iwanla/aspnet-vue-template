using AspNetVue.Server.Authentication;
using AspNetVue.Server.Common.Time;
using AspNetVue.Server.Database;
using AspNetVue.Server.Database.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace AspNetVue.Server.UnitTests.Common;

public sealed class QueryFilterModelTests
{
    [Fact]
    public void Model_Should_ValidateRequiredRelationshipsWithoutQueryFilterWarnings()
    {
        // Validate the real provider's model without opening a database connection.
        DbContextOptions<ApplicationDbContext> options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql("Host=localhost;Database=model-validation")
            .ConfigureWarnings(warnings => warnings.Throw(
                CoreEventId.PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning))
            .Options;

        using var context = new ApplicationDbContext(
            options,
            Substitute.For<IDomainEventsDispatcher>(),
            Substitute.For<IDateTimeProvider>(),
            Substitute.For<IUserContext>());

        context.Model.FindEntityType(typeof(UserRole)).ShouldNotBeNull()
            .GetForeignKeys().ShouldAllBe(foreignKey => foreignKey.IsRequired);
        context.Model.FindEntityType(typeof(RolePermission)).ShouldNotBeNull()
            .GetForeignKeys().ShouldAllBe(foreignKey => foreignKey.IsRequired);
    }
}
