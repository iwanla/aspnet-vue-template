using AspNetVue.Server.Database.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AspNetVue.Server.Features.Todos;

/// <summary>
/// EF configuration for <see cref="TodoItem"/>. It lives with the feature that owns the entity
/// (not in <c>Database/Configurations/</c>) so <c>Database/</c> never references <c>Features/</c>;
/// <c>ApplyConfigurationsFromAssembly</c> finds it on the same assembly.
/// </summary>
internal sealed class TodoItemConfiguration : IEntityTypeConfiguration<TodoItem>
{
    public void Configure(EntityTypeBuilder<TodoItem> builder)
    {
        // Explicit: the table name no longer comes from a DbSet property on ApplicationDbContext
        // (that would force Database/ to reference Features/), so the feature config owns it and
        // the physical table stays `todo_items`.
        builder.ToTable("todo_items");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.DueDate).HasConversion(d => d != null ? DateTime.SpecifyKind(d.Value, DateTimeKind.Utc) : d, v => v);

        builder.HasOne<User>().WithMany().HasForeignKey(t => t.UserId);
    }
}
