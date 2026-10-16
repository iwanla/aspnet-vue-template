using AspNetVue.Server.Common.Messaging;

namespace AspNetVue.Server.Features.Todos;

public sealed record GetTodoByIdQuery(Guid TodoItemId) : IQuery<GetTodoByIdResponse>;
