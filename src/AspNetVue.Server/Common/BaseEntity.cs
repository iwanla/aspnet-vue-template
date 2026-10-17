namespace AspNetVue.Server.Common;

/// <summary>
/// Base type for audited, persisted entities. Adds the surrogate key and the
/// create/update/delete audit columns on top of <see cref="Entity"/>'s domain-event support.
/// Deletes are soft: the row is kept and hidden by the global query filter applied in
/// <c>ApplicationDbContext</c>, so <see cref="DeletedAt"/> records when it happened.
/// </summary>
public abstract class BaseEntity : Entity
{
    public Guid Id { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public string? CreatedByName { get; set; }

    public Guid? UpdatedBy { get; set; }

    public string? UpdatedByName { get; set; }

    public Guid? DeletedBy { get; set; }

    public string? DeletedByName { get; set; }

    /// <summary>Not mapped: the row is hidden by the query filter, this is for in-memory use only.</summary>
    public bool IsDeleted => DeletedAt is not null;
}
