using AspNetVue.Server.Common.Messaging;

namespace AspNetVue.Server.Features.Todos.DeleteTodo;

public sealed record DeleteTodoCommand(Guid TodoItemId) : ICommand;
