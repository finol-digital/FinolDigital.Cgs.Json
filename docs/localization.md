# Authored game-content localization

Localization is optional. Existing `cgs.json`, card data, and deck files retain their
canonical names, IDs, values, and URL substitutions. Translations require authored
catalogs; changing the UI language does not automatically translate a game.

The library format and presentation API are separate from CGS's Unity integration.
A CGS release implementing the consumer contract is required before these catalogs
can change the application's display. Track that work in the OpenSpec change;
publishing the library alone does not deliver the UI feature.

## Declare languages

```json
{
  "name": "Element Cards",
  "localization": {
    "defaultLocale": "en",
    "locales": {
      "es": { "url": "locales/es.json" },
      "ja": { "inline": { "game": { "name": "エレメントカード" } } }
    }
  }
}
```

`defaultLocale` describes the existing authored language and is required when the
manifest is present. `locales` may be absent or empty. Tags use [BCP 47](https://www.rfc-editor.org/rfc/rfc5646);
case-insensitively duplicate keys are invalid. Each source must have exactly one
nonempty `url` or an `inline` object. Neither English nor a source-language catalog
is required. Empty catalogs and empty translation strings are valid. Null or
non-string translations are invalid, not empty strings.

Each leaf resolves through the chosen locale and its parents, then `defaultLocale`
and its parents, then the exact legacy display. `es-MX` can fall back to `es`;
`pt` cannot select `pt-BR`, and `zh-Hant` cannot select `zh-Hans`. No English
fallback is implied. Extensions are truncated without leaving trailing singletons.
CGS currently selects `en es fr de it ja ko pt-BR ru zh-Hans`; other tags are useful
for other consumers, default fallbacks, and future selectors. Unreachable tags
should be diagnosed, not silently matched to sibling locales.

## Catalog members

All entries below are optional. Entity keys are exact, case-sensitive canonical
keys. Keep their original spelling, including punctuation, Unicode, and generated
card-face or printing suffixes. Property paths are resolved lookup keys, not JSON
pointers or translated labels. Enumerate loaded targets instead of guessing paths
from the source JSON. JSON pointers used by the library's diagnostics and inventory
are an output addressing convention, not the property-path syntax in catalogs.

| Member | Key and value |
| --- | --- |
| `game` | `name`, `copyright`, `gamePlayDeckName`, `setNameDefault` string fields |
| `cards` | Final card ID → object with optional `name` and `properties` path → string map |
| `cardNames` | Canonical card name → shared name translation for reprints |
| `sets` | Set code → object with optional `name` |
| `decks` | Authored deck name → object with optional `name` |
| `properties` | Resolved path → optional `display`, `displayEmpty`, `booleanValues` |
| `properties[path].booleanValues` | Optional `true` and `false` string fields |
| `enums` | Resolved property path → canonical member or observed literal token → label |
| `extraGroups` | Canonical extra-group name → label |
| `zones` | Canonical zone name → label |
| `boards` | Board ID → object with optional `name` |
| `cardBacks` | Card-back ID → object with optional `name` |
| `assets.cardImages` | Final card ID → URI reference |
| `assets.cardBacks` | Card-back ID → URI reference |
| `assets.boards` | Board ID → URI reference |
| `assets` singleton fields | `bannerImageUrl`, `cardBackImageUrl`, `playMatImageUrl`, `rulesUrl` URI references |

Within each locale, `cards[id].name` wins over `cardNames[canonicalName]` before
trying another locale. Back faces use their own names. Name-mirroring properties
use the same card-name resolution. Name fallback never applies to card text or
artwork. Two unrelated cards sharing a canonical name can use exact-ID overrides.

`game.setNameDefault` is the sole target for the default set. A competing
`sets[setCodeDefault].name` is ignored and diagnosed, even in a more specific
locale. An empty canonical `gamePlayDeckName` is a sentinel: use the resolved deck
name. A translation cannot turn it into a fixed stack label.

Per-card property overrides apply only to string, escaped-string, string-list,
and textual children. An aggregate list and its separately rendered indexed
children have separate resolved targets. Translations are final display text:
do not decode source escapes or perform template substitution again. Preserve
normal UI markup safety and line breaks. Integer display stays canonical.

Empty canonical values use `displayEmpty` and keep their original emptiness and
filter flags. Enum labels are resolved after the existing member/flag calculation,
joined in the existing order with ` | `. Observed unmapped nonnumeric literal
tokens can be translated; translations must never register members, allocate bits,
or expose unknown numeric flags. Booleans use the canonical true/false parser;
missing labels preserve the raw source display (`yes`, `1`, `true`, etc.).

User-created or renamed decks and stacks keep their own text. A matching name or
card list does not prove authorship. CGS must establish the optional provenance
binding specified in the consumer design before resolving an authored deck label.
Older peers without negotiated provenance retain canonical shared-stack labels.
Full-session save/load is not currently a CGS feature, and this change does not add
it. Session provenance sidecars are conditional on a future save/load feature;
existing deck files and the live multiplayer provenance contract remain in scope.

## References, loading, and offline content

Localization references may be HTTP(S) URLs or relative references. Existing URL
fields, including `allCardsUrl`, retain their absolute-URI contract. New resource
references are literal overrides, never templates using translated names.

Resolve a catalog or inline asset against the definition's recorded final download
URL; resolve an external catalog's assets against that catalog's recorded final
URL. `autoUpdateUrl` and the game's host-derived ID cannot reconstruct that origin.
For example, `locales/es.json` at `https://example.invalid/game/cgs.json` resolves
to `https://example.invalid/game/locales/es.json`. Its `images/001.png` resolves
to `https://example.invalid/game/locales/images/001.png`.

For WebGL downloads through `cgs.games/api/proxy`, the companion proxy returns
`X-CGS-Final-URL` after validating redirects and exposes it through CORS. CGS uses
that value only from its trusted proxy endpoint; missing/invalid headers leave the
remote origin unknown. The companion proxy change must be deployed before relative
remote catalogs can work through that endpoint. Absolute and bundled references
retain their fallback behavior with older proxy responses.

Bundled resources resolve from their actual package location. Reject `file:`
references, absolute filesystem paths, and decoded/normalized or symlink-resolved
paths escaping the game directory. A parent reference staying inside the package
is valid. If an old installation has no origin, use available bundled resources
or fall back; never guess the remote host root.

CGS owns network access and stores catalog/image caches and origin mappings in the
game's `localization/` directory so game packages remain portable offline. Follow
`autoUpdate`, including no automatic downloads when negative. Try available locale
variants and then the original asset. Keep existing artwork while loading, and
preserve formats and geometry. Localized rulebooks select a link; CGS need not
mirror external rulebook websites. Cache ordering, atomic publication, refresh
rollback, provenance, and multiplayer verification are downstream requirements.

## Validation and compatibility

Validate manifests against [cgs.json](../schema/cgs.json) and standalone catalogs
against [GameLocalization.json](../schema/GameLocalization.json), with URI format
checking enabled. The schema enforces structure; semantic validation additionally
checks locale tags, duplicate locales, known targets, eligible property types,
conflicting default-set targets, duplicate labels, and resource restrictions.

`LocalizationJson.ReadCatalog` returns diagnostics and a usable catalog when only
individual leaves are invalid. Unusable structure makes the catalog unavailable.
`ReadManifest` validates manifest sources and tags. `CardGame.Localization` uses a
safe converter so invalid optional metadata does not prevent base-game parsing;
inspect its `IsUsable` and `Diagnostics`. Do not use ordinary catalog deserialization
as the validation boundary: Newtonsoft may otherwise coerce non-string values.
Unknown targets never create entities; missing translations fall back to base data.
When a `CardGame` is serialized without changes to its localization model, its
authored localization JSON is retained, including unknown members and invalid
values. Editing the localization model writes the edited model instead; replacing
or clearing `Localization` also takes effect. This preservation applies to the
`CardGame.Localization` converter, not the cleaned `ReadManifest` result.

Older tolerant readers can use the base content, but old strict schemas reject
`localization`. **Old CGS editors may silently discard localization when saving.**
Keep your source manifest or use an editor that preserves the new metadata.
Removing/nulling localization on a successful reload disables bindings; CGS must
stage and validate the new definition before publishing it and retain last-good
state if loading fails. Existing saves, deck formats, and IDs require no migration.

The [example game](../examples/localization/README.md)
contains Spanish/Japanese names, properties, booleans, and artwork references.

## Library presentation API

Build `LocalizationTargets` from the **fully loaded** canonical game, cards, sets,
and authored deck names. Supply `resolvedProperties` when the consumer has
additional flattened/indexed property controls not present on a loaded card.
Supply `boardIds`/`cardBackIds` for package-discovered resources without URL entries;
board references in `gameBoardCards` are also enumerated.
Cards expose their final parsed properties through `CloneProperties`; no source
JSON layout or generated ID suffix needs to be guessed. Rebuild this inventory
when the canonical data changes. `Leaves` lists JSON-pointer addresses and base
values; `Validate(catalog)` reports unknown/ineligible targets and
`Coverage(catalog)` lists missing and unknown leaves. Coverage accounts for shared
card-name translations. Diagnostics before all data loads are provisional.

Create a `LocalizationContext` for each locale and current manifest/catalog
snapshot. It reads inline catalogs from `game.Localization`. Supply downloaded or
cached catalogs in `externalCatalogs`, keyed by declared locale, only after matching
them to the manifest's current source URI. The consumer owns cache identity,
fetching, generation checks, and publication order. The library performs no network
requests, consults no global locale, and does not mutate canonical objects.

```csharp
var game = new CardGame("Example")
{
    Localization = LocalizationJson.ReadManifest(
        "{\"defaultLocale\":\"en\",\"locales\":{\"es\":{\"inline\":{\"cardNames\":{\"Flame\":\"Llama\"}}}}}").Value
};
var card = new Card(game, "001", "Flame", "SET",
    new Dictionary<string, PropertyDefValuePair>(), false);
var targets = new LocalizationTargets(game, new[] { card });
var display = new LocalizationContext(targets, "es-MX");
var label = display.CardName(card); // Llama
var canonical = card.Name;         // Flame
```

The presentation methods cover game/copyright, card and set names, property/empty
labels, property values, enum options, zones, groups, boards, and card backs.
`DeckName(name, hasAuthoredProvenance)` translates only when the caller has verified
the authored binding. Never set that argument merely because names or cards match.
`GamePlayDeckName(resolvedDeckName)` preserves the canonical empty-label sentinel.
An explicit user rename clears the binding in the consumer. `EnumLabel` receives
the **canonical member token**, not the currently displayed label.

Call `AssetCandidates("cardImages", card.Id)` (or the corresponding asset member)
to obtain ordered localized candidates. Try each until usable, then use the
existing canonical resource. Candidates carry locale, original reference, and
resolved URI. `LocalizationCatalogDocument` pairs an external catalog with its
actual origin. Pass `definitionOrigin` for inline references. Use
`LocalizationOrigin.Remote(finalResponseUrl)` for remote documents; it never reads
`AutoUpdateUrl`. `LocalizationOrigin.Resolve(source.Url, definitionOrigin)` also
resolves external catalog references.

For bundled documents, use `LocalizationOrigin.Bundled(gameDirectory,
absoluteDocumentPath, physicalPathResolver)`. The callback must resolve **every
ancestor symlink** to an absolute physical path for an existing file/directory,
or return null when unavailable; resolving just the final component is insufficient.
The library rejects lexical and physical escapes from the resolved game directory.
The callback is a consumer filesystem adapter because .NET Standard 2.1 lacks a
portable symlink-resolution API. The returned internal file URI is usable only
after those checks; author-supplied `file:` references are still rejected.
Protect the package from concurrent filesystem replacement while opening a
validated resource. Package installation and access synchronization belong to the
consumer; this path validation is not an operating-system sandbox.

Keep canonical paths/flags as control values; never reverse-map labels to keys.
Interactive search can use these display methods, but leave imports, automatic
card queries, exports, and gameplay sorting on existing canonical APIs. Recreate
contexts on language changes and accepted catalog/manifest updates, then refresh
views without modifying gameplay state.

## Running checks

The library still targets .NET Standard 2.1. The test project uses .NET 10.
Run `dotnet test FinolDigital.Cgs.Json.Tests`. Generate both schema files with
`dotnet run --project SchemaGeneration -- schema` using the project's .NET 6
runtime, or set `DOTNET_ROLL_FORWARD=Major` when using a newer runtime locally.
Install `tests/requirements.txt` in a Python environment, then run
`python3 -m unittest discover -s tests` for schema/URI-format and example checks.
