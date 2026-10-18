using AspNetVue.Server.Common.Domain;

namespace AspNetVue.Server.Features.Todos;

public sealed record TodoItemUpdatedDomainEvent(Guid TodoItemId) : IDomainEvent;
