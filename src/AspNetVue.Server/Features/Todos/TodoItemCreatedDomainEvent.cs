using AspNetVue.Server.Common.Domain;

namespace AspNetVue.Server.Features.Todos;

public sealed record TodoItemCreatedDomainEvent(Guid TodoItemId) : IDomainEvent;
