using AspNetVue.Server.Common;

namespace AspNetVue.Server.Features.Todos;

public sealed record TodoItemCompletedDomainEvent(Guid TodoItemId) : IDomainEvent;
