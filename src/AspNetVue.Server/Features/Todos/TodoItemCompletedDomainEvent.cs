using AspNetVue.Server.Common.Domain;

namespace AspNetVue.Server.Features.Todos;

public sealed record TodoItemCompletedDomainEvent(Guid TodoItemId) : IDomainEvent;
