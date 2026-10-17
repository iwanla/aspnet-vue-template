namespace AspNetVue.Server.Features.Todos.CreateTodo;

public sealed class CreateTodoRequest
{
    public Guid UserId { get; set; }
    public string Description { get; set; }
    public DateTime? DueDate { get; set; }
    public List<string> Labels { get; set; } = [];
    public int Priority { get; set; }
}
