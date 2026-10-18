using System.Globalization;

namespace AspNetVue.Server.UnitTests.Common.Localization;

internal sealed class CultureScope : IDisposable
{
    private readonly CultureInfo _culture = CultureInfo.CurrentCulture;
    private readonly CultureInfo _uiCulture = CultureInfo.CurrentUICulture;

    public CultureScope(string uiCulture, string? culture = null)
    {
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture ?? uiCulture);
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(uiCulture);
    }

    public void Dispose()
    {
        CultureInfo.CurrentCulture = _culture;
        CultureInfo.CurrentUICulture = _uiCulture;
    }
}
