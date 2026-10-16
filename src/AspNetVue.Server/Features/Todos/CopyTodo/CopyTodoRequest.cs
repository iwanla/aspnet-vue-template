namespace AspNetVue.Server.Features.Todos;

public sealed class CopyTodoRequest
{
    public Guid UserId { get; set; }
    public Guid TodoId { get; set; }
}
