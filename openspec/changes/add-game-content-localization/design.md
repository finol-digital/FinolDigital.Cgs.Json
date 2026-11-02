# Design

## Context

See [proposal.md](proposal.md) for motivation. Current code couples several displayed strings to behavior:

- `CardGame.Id` derives from `Name` and the update host. `Card.Name` participates in reprint detection and deck serialization.
- `Card.ImageWebUrl` substitutes canonical names and property values into URLs. Deck exports use names, set codes, alternate IDs, and extra-group keys.
- `PropertyDef.Name` identifies source data. `EnumDef.Values` maps canonical keys to display strings; `Lookups` maps those keys to bit flags.
- Deck URLs use `Name` in local filenames; zones and extra groups have names without separate IDs.
- `SchemaGeneration/Program.cs` generates `schema/cgs.json` from Newtonsoft-annotated types with camel-case serialization. The schema rejects unknown properties.
- In the adjacent CGS checkout, `CgsLocalization.Changed` signals language changes and Unity Localization holds the selected locale. The prior UI-localization proposal excluded game data and artwork.

No runtime or published schema changes are part of this proposal artifact.

## Goals / Non-Goals

**Goals:** Separate canonical data from presentation, allow small or large opt-in catalogs, and define consistent library and Unity behavior. Every game-authored display string has a translation target; a complete authored catalog can localize all such strings. Missing content falls back safely.

**Non-goals:** Runtime machine translation, OCR, translating chat or user edits, changing the language selector's supported locales, replacing external card APIs, plural/message-template infrastructure, or changing deck/network formats. An untranslated game does not become translated merely by changing the UI language.

## Decisions

### 1. Optional manifest with inline or external catalogs

`CardGame.Localization` is nullable and omitted when absent. Its proposed JSON shape is:

```json
{
  "name": "Example Game",
  "localization": {
    "defaultLocale": "en",
    "locales": {
      "es": { "url": "locales/es.json" },
      "ja": { "inline": { "game": { "name": "サンプルゲーム" } } }
    }
  }
}
```

`defaultLocale` is required when `localization` is present and describes existing authored content. `locales` is optional, empty by default. Keys and `defaultLocale` are BCP 47 language tags. Each locale source has exactly one of `url` (nonempty URI reference) or `inline` (catalog object). Empty catalogs are valid. No source-language or English catalog is required.

External catalogs keep large translations out of the manifest; inline catalogs support small/offline games. One catalog schema covers both. No recursive includes are supported. Resolve relative references against the containing document's origin; local installations resolve them within the game directory. Downloaded definitions retain their origin for later resolution. An unresolvable reference is unavailable and falls back.

Alternatives rejected: replacing strings with language maps breaks existing readers and external JSON integration; complete translated game copies duplicate state and risk divergent IDs; arbitrary JSON patches allow translations to change gameplay.

### 2. Explicit translation targets keyed by existing identity

A catalog is a partial object with the following optional members. Dictionary keys are exact and case-sensitive; preserve their spelling when serializing rather than applying the property-name casing policy. Unknown targets produce diagnostics and never create entities.

| Member | Key / value | Target |
| --- | --- | --- |
| `game` | `name`, `copyright`, `gamePlayDeckName`, `setNameDefault` strings | Corresponding game display strings; default set label only where CGS already uses it. |
| `cards` | Card ID → `{ "name": string, "properties": { path: string } }` | Final parsed card ID, including distinct generated reprint/back-face IDs; name and textual property display. |
| `sets` | Set code → `{ "name": string }` | Canonical set code from any supported source. |
| `decks` | Authored deck name → `{ "name": string }` | Decks from `deckUrls`, `gameStartDecks`, or `AllDecks.json`, including loaded instances. |
| `properties` | Property path → `{ "display": string, "displayEmpty": string }` | Property labels and missing-value labels, including nested children. |
| `enums` | Property path → `{ canonicalEnumKey: string }` | Existing enum members: string, object, list, or hexadecimal flags. |
| `extraGroups` | Authored group name → string | `ExtraDef.Group` label. |
| `zones` | Authored zone name → string | `GamePlayZone.Name` label. |
| `boards` | Board ID → `{ "name": string }` | Label wherever a board ID is displayed. |
| `cardBacks` | Back ID → `{ "name": string }` | Label wherever a card-back ID is displayed. |
| `assets` | Resource references, defined below | Localized presentation assets. |

Property paths are existing canonical lookup keys after parsing, not localized labels or JSON pointers: e.g., `rules`, `stats.attack`, and flattened object-list keys. An authoring/export helper must enumerate resolved IDs and paths, including generated face IDs and indexed/aggregate object-list paths. These targets are independent of whether data came from `AllCards.json`, nested sets, per-set downloads, or custom identifiers.

`cards[id].properties[path]` is a final display-string override for `string`, `escapedString`, `stringList`, and textual children of objects/lists. An aggregate-list override replaces only that rendered aggregate; indexed children are separate targets if rendered separately. It never replaces parsed values or structure. Apply normal UI text safety/markup rules, without repeating source escape decoding or template substitution. Enums use `enums` instead of per-card overrides. Numeric values remain numeric; boolean presentation uses existing UI-localized labels. Unknown/ineligible overrides are diagnosed and ignored.

Enum translation follows canonical key/flag resolution and preserves existing order and composition. Resolve each member's translation independently, then join with the existing presentation delimiter. Translate `displayEmpty` without changing emptiness, `displayEmptyFirst`, or filter semantics. An empty translation string is deliberate and valid. Missing entries continue fallback; null/non-string leaves are invalid, diagnosed, and treated as absent.

An empty canonical `gamePlayDeckName` retains its sentinel behavior: show the resolved deck name. A translation cannot turn it into a fixed stack label. User-renamed decks lose the authored-name display binding. Duplicate canonical deck/zone/group names share a translation, consistent with today's identity model. Independent IDs would require a separate change.

Alternative: global source-text replacement cannot disambiguate identical strings used for different entities. Mandatory new deck/zone IDs would require migrations.

### 3. Deterministic per-leaf fallback

The library resolver receives an explicit UI locale, manifest, and available catalogs; it does not consult process culture or Unity globals. Match tags case-insensitively. CGS normalizes its UI aliases to canonical tags before calling the resolver. Reject manifest keys that collide case-insensitively.

Build a deduplicated chain of the selected tag and progressively less specific parents, then `defaultLocale` and its parents. Truncation removes the rightmost subtag and any resulting trailing singleton so extension sequences never become malformed candidates. Only declared catalogs are used. Do not infer siblings/scripts (`pt` does not select `pt-BR`; `zh-Hant` does not select `zh-Hans`). Resolve each leaf from the first valid available translation, then the exact legacy display result. For `es-MX` with default `ja`: `es-MX`, `es`, `ja`, legacy. An invalid/missing UI locale starts at the game default. Absent metadata uses only legacy behavior.

Property display precedence: empty canonical value → localized empty label or legacy empty result; enum → localized resolved members; eligible nonempty text → per-card override or legacy result; other type → existing typed presentation. A displayed property mirroring the configured card name uses the same resolved card-name target to avoid disagreement between views. Source-language catalogs can override display text too, without altering base identity.

### 4. Add a display API; preserve existing APIs

Add Unity-independent catalog models and a resolver in this library. Display operations accept canonical objects/keys and a locale/catalog context. Keep `Name`, `PropertyDef.Display`, `EnumDef.Values`, existing getters including `GetPropertyValueString`, `ImageWebUrl`, and serialization behavior unchanged. Do not make existing getters language-dependent.

CGS calls the display API at rendering/search boundaries: explorer, viewers, deck lists/editor, filters, menus, boards, zones, extra stacks, and dynamic messages embedding game text. Bind messages to canonical references so active messages can refresh. Rebuilt controls retain canonical selected values. Search matches both original and current translated names/text; filters compare canonical values/flags. Presentation sorting never reorders gameplay state.

Switching languages cannot rename files or mutate models, reprint groups, IDs, import/export output, URL substitutions, or network payloads. Participants can use different languages in one session. CGS-owned message templates continue to use CGS's UI catalog; chat and arbitrary user strings are preserved.

### 5. Optional localized artwork and rulebooks

String catalogs cannot change text inside images. `assets` supports `cardImages` (card ID → URI reference), `cardBacks` (back ID → URI reference), `boards` (board ID → URI reference), and singleton `bannerImageUrl`, `cardBackImageUrl`, `playMatImageUrl`, `rulesUrl`. Image variants use the corresponding existing game image format and do not change dimensions, board geometry, or rules.

References are explicit overrides, not templates; never substitute translated names into legacy URLs. Resolve inline references against the definition origin and external-catalog references against that catalog's origin. Image loading follows the locale chain; an unavailable/invalid variant tries the next candidate then the existing asset. Cache variants separately and keep existing artwork visible while replacements load. A localized rulebook changes the link CGS opens; its external content/availability remains the author's responsibility.

This permits complete authored localization without OCR/image generation; text-only catalogs retain original artwork.

### 6. CGS owns asynchronous loading and refresh

The library owns format/resolution; Unity owns I/O and events. Subscribe to `CgsLocalization.Changed` and use Unity Localization's selected locale. At startup, game change, or language change, resolve from inline/cache data immediately and load needed fallback-chain catalogs asynchronously. Refresh active views on completion; new views use the current locale. Never reload gameplay state or reset selections.

Cache by canonical game identity, normalized locale, and resolved source URI. Follow `autoUpdate`, including `< 0` meaning no automatic downloads. Keep the last successfully validated catalog for that source on refresh failure; changed manifests/sources invalidate affected bindings. Without cache, try the next fallback and then base data. Locale changes must not redownload canonical cards/decks/sets. Rapid language/game switches discard stale callbacks for display; old requests can populate only their own caches.

Assets also include target and resource URI in cache identity. Bundled catalogs/assets work offline. Invalid document structure rejects the catalog; invalid leaves/unknown targets are ignored with diagnostics while valid siblings remain usable. Authoring validation is strict. No translation failure prevents loading/playing the base game.

### 7. Generated schemas and compatibility checks

Extend schema generation for `cgs.json` and `GameLocalization.json`, URI references, and exactly-one inline/URL constraints. Tags, normalized duplicates, and reference existence also require semantic validation; JSON Schema cannot establish IDs in external card data.

Keep existing required fields/string shapes unchanged and omit unset localization on serialization. Regression fixtures cover original English, Spanish, and Japanese definitions, custom identifiers, all deck formats, nested/lists, flags, back faces, and asset templates. Compare parsed values/output before and after. Unchanged fixtures must still pass the old schema; localized manifests are expected to fail old strict schemas. Unknown-member-tolerant readers can continue using base data, but acceptance by every historical consumer is not promised.

## Risks / Trade-offs

- Missed UI call sites → Inventory CGS surfaces; test active dialogs/messages as well as newly opened views. Schema delivery alone is not completion.
- Authors rename canonical keys → Export actual targets and report orphans; never rebind using translated text.
- Partial catalogs mix languages → Per-leaf fallback keeps games playable; coverage reports identify untranslated work.
- Artwork adds download/cache size → Optional, lazy loading with existing cache policies.
- Stale callbacks/mixed-language sessions → Scope cache and refresh identities to game/locale/source/request generation; keep translations out of shared state.
- New scripts need glyph/layout support → Reuse CGS font fallbacks and verify multiline text, long labels, and supported scripts downstream.

## Migration Plan

1. Implement/test additive library models, schemas, resolver, and authoring docs here.
2. Adopt the package in CGS and implement/verify the consumer contract. Track that work in a corresponding CGS change referencing this contract.
3. Validate opt-in examples, missing languages, offline loading, and mixed-language multiplayer before release.
4. Release CGS support before advertising localized manifests. Existing games require no edits. Removing `localization` disables the feature without changing source data, saves, or assets. Old strict-validator distributions require the original manifest.

## Example Provenance

The user-provided [Suma o Muere](https://cgs.games/atoky/suma_o_muere) and [Hokuto no Ken](https://cgs.games/themadness/%E5%8C%97%E6%96%97%E3%81%AE%E6%8B%B3%E4%B8%96%E7%B4%80%E6%9C%AB%E3%82%AB%E3%83%BC%E3%83%89%E3%83%90%E3%83%88%E3%83%AB%E4%BC%9D%E8%AA%AC_hokuto_no_ken_card_battle) motivate non-English base-language coverage. Web fetch could not access their pages during drafting; no assumptions about their exact manifests are needed. The [examples](examples/README.md) are synthetic, not edits to those games.
