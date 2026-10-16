using AspNetVue.Server.Common;

namespace AspNetVue.Server.Features.Todos;

public sealed record TodoItemCreatedDomainEvent(Guid TodoItemId) : IDomainEvent;
