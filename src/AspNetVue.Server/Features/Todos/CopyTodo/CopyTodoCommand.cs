using AspNetVue.Server.Common.Messaging;

namespace AspNetVue.Server.Features.Todos;

public sealed class CopyTodoCommand : ICommand<Guid>
{
    public Guid UserId { get; set; }
    public Guid TodoId { get; set; }
}
