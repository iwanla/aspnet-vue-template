using AspNetVue.Server.Database.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AspNetVue.Server.Database.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.Id);

        // Partial: a soft-deleted row keeps its value, so only active rows must be unique.
        builder.HasIndex(u => u.Email).IsUnique().HasFilter("deleted_at IS NULL");
    }
}
