namespace AspNetVue.Server.Common;

public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}
