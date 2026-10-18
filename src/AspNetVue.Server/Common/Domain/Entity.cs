namespace AspNetVue.Server.Common.Domain;

/// <summary>
/// Adds domain-event support to a domain object. Persisted entities derive from
/// <see cref="BaseEntity"/>, which also supplies identity and the audit columns.
/// </summary>
public abstract class Entity
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public List<IDomainEvent> DomainEvents => [.. _domainEvents];

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    public void Raise(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }
}
