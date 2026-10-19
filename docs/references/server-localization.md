# Server Localization

For server/client ownership boundaries, see
[Application Architecture](../../ARCHITECTURE.md). Server paths below are relative
to `src/AspNetVue.Server/`.

## Scope and culture selection

`Common/Localization/` uses built-in .NET RESX localization for server-generated
text such as email, PDFs, and notifications. UI translations remain client-owned;
resource files are not shared merely because their text is similar. Existing API
error codes/descriptions are not translated by this infrastructure.

`AddServerLocalization()` registers `AddLocalization()` and configures request
culture selection using only `Accept-Language`. English (`en`) is the default and
Indonesian (`id`) is also supported. Regional variants negotiate their supported
parent; unsupported languages fall back to English. `Program.cs` installs request
localization before request consumers. No user locale/database lookup is involved.
For background work without a request, establish the recipient's culture
explicitly; the request header is not a persisted user preference.

## Feature-owned resources

Place a marker and matching RESX files side by side in the feature namespace:

```text
Features/Notes/
  NotesResources.cs          public sealed class NotesResources;
  NotesResources.resx        Neutral English strings
  NotesResources.id.resx     Indonesian strings
```

Inject `IStringLocalizer<NotesResources>` from `Microsoft.Extensions.Localization`
(or the equivalent marker for the owning feature):

```csharp
string message = localizer["notes.notification.created", note.Title];
```

Use stable `<feature>.<usecase>.<key>` keys and matching placeholders across locales.
Do not put HTML markup with unescaped user input in resources.
`Common/Localization` must never reference concrete feature types.

The marker namespace must match the resource's folder; there is no namespace-parent
discovery or custom factory. Do not configure `ResourcesPath` for this colocated
layout. SDK defaults embed neutral resources in the main assembly and translations
in satellite assemblies, included in build and publish output. Resource edits
require a rebuild.

## Lookup and fallback

The built-in resource manager handles caching and culture-parent fallback, ending
at the neutral English resources. This fallback is independent of the configured
request default culture. Lookup uses `CurrentUICulture`; formatting uses
`CurrentCulture` and `{0}`-style placeholders.

Missing keys return their names with `ResourceNotFound = true`; successful fallback
is not missing. No custom missing-key warning logs are provided.
`GetAllStrings(true)` merges parents and neutral resources; `false` excludes parents
and can throw if the exact culture has no resource set.

## Verification

Test supported, regional, and unsupported request cultures; neutral fallback;
missing keys; matching keys/placeholders across locales; and formatted output.
Verify translated satellite assemblies are included in published output.
