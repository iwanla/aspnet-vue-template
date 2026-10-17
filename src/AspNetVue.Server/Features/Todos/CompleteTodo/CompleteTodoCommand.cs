using AspNetVue.Server.Common.Messaging;

namespace AspNetVue.Server.Features.Todos.CompleteTodo;

public sealed record CompleteTodoCommand(Guid TodoItemId) : ICommand;
