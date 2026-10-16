using AspNetVue.Server.Common.Messaging;

namespace AspNetVue.Server.Features.Todos;

public sealed record UpdateTodoCommand(
    Guid TodoItemId,
    string Description) : ICommand;
