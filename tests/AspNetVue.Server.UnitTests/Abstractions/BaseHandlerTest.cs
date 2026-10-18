using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using AspNetVue.Server.Authentication;
using AspNetVue.Server.Common.Time;
using AspNetVue.Server.Database;

namespace AspNetVue.Server.UnitTests.Abstractions;

public abstract class BaseHandlerTest
{
    protected static ApplicationDbContext CreateDbContext(
        IDomainEventsDispatcher? domainEventsDispatcher = null,
        IDateTimeProvider? dateTimeProvider = null,
        IUserContext? userContext = null)
    {
        DbContextOptions<ApplicationDbContext> options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"vertical-slice-{Guid.NewGuid()}")
            .Options;

        IUserContext resolvedUserContext = userContext ?? CreateAnonymousUserContext();

        return new ApplicationDbContext(
            options,
            domainEventsDispatcher ?? Substitute.For<IDomainEventsDispatcher>(),
            dateTimeProvider ?? Substitute.For<IDateTimeProvider>(),
            resolvedUserContext);
    }

    private static IUserContext CreateAnonymousUserContext()
    {
        // NSubstitute auto-values strings as "", but "no authenticated request" means null.
        IUserContext userContext = Substitute.For<IUserContext>();
        userContext.Name.Returns((string?)null);
        return userContext;
    }

    protected static HybridCache CreateCache()
    {
        var services = new ServiceCollection();
#pragma warning disable EXTEXP0018
        services.AddHybridCache();
#pragma warning restore EXTEXP0018
        return services.BuildServiceProvider().GetRequiredService<HybridCache>();
    }
}
