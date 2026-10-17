namespace AspNetVue.Server.Features.Todos.CopyTodo;

public sealed class CopyTodoRequest
{
    public Guid UserId { get; set; }
    public Guid TodoId { get; set; }
}
