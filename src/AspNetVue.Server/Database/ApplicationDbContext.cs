using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using AspNetVue.Server.Authentication;
using AspNetVue.Server.Common.Domain;
using AspNetVue.Server.Common.Time;
using AspNetVue.Server.Database.Entities.Identity;

namespace AspNetVue.Server.Database;

public sealed class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options,
    IDomainEventsDispatcher domainEventsDispatcher,
    IDateTimeProvider dateTimeProvider,
    IUserContext userContext)
    : DbContext(options)
{
    public DbSet<User> Users { get; set; }

    public DbSet<RefreshToken> RefreshTokens { get; set; }

    public DbSet<Role> Roles { get; set; }

    public DbSet<Permission> Permissions { get; set; }

    public DbSet<RolePermission> RolePermissions { get; set; }

    public DbSet<UserRole> UserRoles { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        modelBuilder.HasDefaultSchema(Schemas.Default);

        ApplySoftDeleteFilters(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Audit and soft-delete stamping happen before saving so the values are written in the
        // same round trip. Domain events follow the same split described below.

        ApplyAuditStamps();

        // When should you publish domain events?
        //
        // 1. BEFORE calling SaveChangesAsync
        //     - domain events are part of the same transaction
        //     - immediate consistency
        // 2. AFTER calling SaveChangesAsync
        //     - domain events are a separate transaction
        //     - eventual consistency
        //     - handlers can fail

        List<IDomainEvent> domainEvents = ExtractDomainEvents();
        int result = await base.SaveChangesAsync(cancellationToken);

        await PublishDomainEventsAsync(domainEvents);

        return result;
    }

    private async Task PublishDomainEventsAsync(IEnumerable<IDomainEvent> domainEvents)
    {
        await domainEventsDispatcher.DispatchAsync(domainEvents);
    }

    private List<IDomainEvent> ExtractDomainEvents()
    {
        var domainEvents = ChangeTracker
            .Entries<Entity>()
            .Select(entry => entry.Entity)
            .SelectMany(entity =>
            {
                List<IDomainEvent> domainEvents = entity.DomainEvents;

                entity.ClearDomainEvents();

                return domainEvents;
            })
            .ToList();
        return domainEvents;
    }

    /// <summary>
    /// Stamps create/update metadata from <see cref="IDateTimeProvider"/> and
    /// <see cref="IUserContext"/>, and turns deletes into soft deletes. The actor is null for
    /// work performed without an authenticated request, such as seeding or registration.
    /// </summary>
    private void ApplyAuditStamps()
    {
        DateTime utcNow = dateTimeProvider.UtcNow;
        Guid? actorId = userContext.UserIdOrNull;
        string? actorName = userContext.Name;

        foreach (EntityEntry<BaseEntity> entry in ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = utcNow;
                    entry.Entity.CreatedBy = actorId;
                    entry.Entity.CreatedByName = actorName;
                    break;

                case EntityState.Modified:
                    entry.Entity.UpdatedAt = utcNow;
                    entry.Entity.UpdatedBy = actorId;
                    entry.Entity.UpdatedByName = actorName;
                    break;

                case EntityState.Deleted:
                    // Soft delete: keep the row, record who/when, and hide it via the query filter.
                    entry.State = EntityState.Modified;
                    entry.Entity.DeletedAt = utcNow;
                    entry.Entity.DeletedBy = actorId;
                    entry.Entity.DeletedByName = actorName;
                    break;
            }
        }
    }

    /// <summary>
    /// Applies <c>deleted_at IS NULL</c> to every audited entity so soft-deleted rows stay out of
    /// queries by default. Feature queries must not re-add a hard-delete filter.
    /// </summary>
    private static void ApplySoftDeleteFilters(ModelBuilder modelBuilder)
    {
        foreach (IMutableEntityType entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                continue;
            }

            modelBuilder.Entity(entityType.ClrType).Ignore(nameof(BaseEntity.IsDeleted));

            ParameterExpression parameter = Expression.Parameter(entityType.ClrType, "entity");
            MemberExpression deletedAt = Expression.Property(parameter, nameof(BaseEntity.DeletedAt));
            BinaryExpression notDeleted = Expression.Equal(
                deletedAt, Expression.Constant(null, typeof(DateTime?)));

            entityType.SetQueryFilter(Expression.Lambda(notDeleted, parameter));
        }
    }
}
