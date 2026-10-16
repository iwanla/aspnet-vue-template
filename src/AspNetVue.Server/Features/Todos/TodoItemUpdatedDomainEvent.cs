using AspNetVue.Server.Common;

namespace AspNetVue.Server.Features.Todos;

public sealed record TodoItemUpdatedDomainEvent(Guid TodoItemId) : IDomainEvent;
