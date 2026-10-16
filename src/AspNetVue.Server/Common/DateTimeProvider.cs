using AspNetVue.Server.Common;

namespace AspNetVue.Server.Common;

internal sealed class DateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
