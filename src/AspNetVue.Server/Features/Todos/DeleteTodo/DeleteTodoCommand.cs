using AspNetVue.Server.Common.Messaging;

namespace AspNetVue.Server.Features.Todos;

public sealed record DeleteTodoCommand(Guid TodoItemId) : ICommand;
