using AspNetVue.Server.Database.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AspNetVue.Server.Database.Configurations;

internal sealed class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.HasKey(permission => permission.Id);

        builder.Property(permission => permission.Name).HasMaxLength(200);

        builder.HasIndex(permission => permission.Name).IsUnique().HasFilter("deleted_at IS NULL");
    }
}
