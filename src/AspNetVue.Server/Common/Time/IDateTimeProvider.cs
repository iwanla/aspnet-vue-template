namespace AspNetVue.Server.Common.Time;

public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}
