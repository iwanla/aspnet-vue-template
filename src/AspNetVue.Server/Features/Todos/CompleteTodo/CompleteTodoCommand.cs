using AspNetVue.Server.Common.Messaging;

namespace AspNetVue.Server.Features.Todos;

public sealed record CompleteTodoCommand(Guid TodoItemId) : ICommand;
