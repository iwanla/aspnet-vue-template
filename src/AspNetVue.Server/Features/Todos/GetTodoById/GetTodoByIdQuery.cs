using AspNetVue.Server.Common.Messaging;

namespace AspNetVue.Server.Features.Todos.GetTodoById;

public sealed record GetTodoByIdQuery(Guid TodoItemId) : IQuery<GetTodoByIdResponse>;
