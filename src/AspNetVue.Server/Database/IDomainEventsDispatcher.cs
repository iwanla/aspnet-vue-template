using AspNetVue.Server.Common.Domain;

namespace AspNetVue.Server.Database;

public interface IDomainEventsDispatcher
{
    Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default);
}
