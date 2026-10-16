using AspNetVue.Server.Common;

namespace AspNetVue.Server.Features.Todos;

public sealed record TodoItemDeletedDomainEvent(Guid TodoItemId) : IDomainEvent;
