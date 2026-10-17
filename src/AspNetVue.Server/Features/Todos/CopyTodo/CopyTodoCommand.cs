using AspNetVue.Server.Common.Messaging;

namespace AspNetVue.Server.Features.Todos.CopyTodo;

public sealed class CopyTodoCommand : ICommand<Guid>
{
    public Guid UserId { get; set; }
    public Guid TodoId { get; set; }
}
