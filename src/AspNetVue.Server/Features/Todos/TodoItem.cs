using AspNetVue.Server.Common.Domain;

namespace AspNetVue.Server.Features.Todos;

public sealed class TodoItem : BaseEntity
{
    public Guid UserId { get; set; }
    public string Description { get; set; }
    public DateTime? DueDate { get; set; }
    public List<string> Labels { get; set; } = [];
    public bool IsCompleted { get; set; }
    public DateTime? CompletedAt { get; set; }
    public Priority Priority { get; set; }
}
