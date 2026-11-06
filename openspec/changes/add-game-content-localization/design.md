# Design

## Context

See [proposal.md](proposal.md) for motivation. Current code couples several displayed strings to behavior:

- `CardGame.Id` derives from `Name` and the update host. `Card.Name` participates in reprint detection and deck serialization.
- `Card.ImageWebUrl` substitutes canonical names and property values into URLs. Deck exports use names, set codes, alternate IDs, and extra-group keys.
- `PropertyDef.Name` identifies source data. `EnumDef.Values` maps canonical keys to display strings; `Lookups` maps those keys to bit flags.
- Deck URLs use `Name` in local filenames; zones and extra groups have names without separate IDs.
- CGS lists downloaded and user-created deck files by name; its stack RPCs carry names and card IDs, without authored-deck provenance. `FilterCards` serves interactive search, deck imports, and automatic `DeckPlayCards` queries. Some current UI controls also use visible labels as lookup keys.
- `SchemaGeneration/Program.cs` generates `schema/cgs.json` from Newtonsoft-annotated types with camel-case serialization. The schema rejects unknown properties.
- In the adjacent CGS checkout, `CgsLocalization.Changed` signals language changes and Unity Localization holds the selected locale. The prior UI-localization proposal excluded game data and artwork.

No runtime or published schema changes are part of this proposal artifact.

## Goals / Non-Goals

**Goals:** Separate canonical data from presentation, allow small or large opt-in catalogs, and define consistent library and Unity behavior. Every game-authored display string has a translation target; a complete authored catalog can localize all such strings. Missing content falls back safely.

**Non-goals:** Runtime machine translation, OCR, translating chat or user edits, changing the language selector's supported locales, replacing external card APIs, plural/message-template infrastructure, or changing existing deck-file/gameplay-message formats. Optional provenance sidecars and a versioned presentation-metadata channel are in scope. An untranslated game does not become translated merely by changing the UI language.

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

External catalogs keep large translations out of the manifest; inline catalogs support small/offline games. One catalog schema covers both. No recursive includes are supported. New catalog/asset references accept HTTP(S) URLs or relative references; existing fields such as `allCardsUrl` keep their current absolute-URI contract. Resolve relative references against the containing document's origin or bundled location, as specified in section 6. An unresolvable reference is unavailable and falls back.

Alternatives rejected: replacing strings with language maps breaks existing readers and external JSON integration; complete translated game copies duplicate state and risk divergent IDs; arbitrary JSON patches allow translations to change gameplay.

### 2. Explicit translation targets keyed by existing identity

A catalog is a partial object with the following optional members. Dictionary keys are exact and case-sensitive; preserve their spelling when serializing rather than applying the property-name casing policy. Unknown targets produce diagnostics and never create entities.

| Member | Key / value | Target |
| --- | --- | --- |
| `game` | `name`, `copyright`, `gamePlayDeckName`, `setNameDefault` strings | Corresponding game display strings; default set label only where CGS already uses it. |
| `cards` | Card ID → `{ "name": string, "properties": { path: string } }` | Final parsed card ID, including distinct generated reprint/back-face IDs; name and textual property display. |
| `cardNames` | Canonical card name → string | Shared name translation for reprints; scoped to card names, not global text replacement. |
| `sets` | Set code → `{ "name": string }` | Canonical set code from any supported source. |
| `decks` | Authored deck name → `{ "name": string }` | Decks from `deckUrls`, `gameStartDecks`, or `AllDecks.json`, including loaded instances. |
| `properties` | Property path → `{ "display": string, "displayEmpty": string, "booleanValues": { "true": string, "false": string } }` | Property labels, missing-value labels, and optional boolean labels, including nested children. |
| `enums` | Property path → `{ canonicalToken: string }` | Existing enum members or unmapped nonnumeric source tokens that the legacy renderer displays literally; string, object, list, or hexadecimal-flag forms. |
| `extraGroups` | Authored group name → string | `ExtraDef.Group` label. |
| `zones` | Authored zone name → string | `GamePlayZone.Name` label. |
| `boards` | Board ID → `{ "name": string }` | Label wherever a board ID is displayed. |
| `cardBacks` | Back ID → `{ "name": string }` | Label wherever a card-back ID is displayed. |
| `assets` | Resource references, defined below | Localized presentation assets. |

Property paths are existing canonical lookup keys after parsing, not localized labels or JSON pointers: e.g., `rules`, `stats.attack`, and flattened object-list keys. An authoring/export helper must enumerate resolved IDs and paths, including generated face IDs and indexed/aggregate object-list paths. These targets are independent of whether data came from `AllCards.json`, nested sets, per-set downloads, or custom identifiers.

`cards[id].properties[path]` is a final display-string override for `string`, `escapedString`, `stringList`, and textual children of objects/lists. An aggregate-list override replaces only that rendered aggregate; indexed children are separate targets if rendered separately. It never replaces parsed values or structure. Apply normal UI text safety/markup rules, without repeating source escape decoding or template substitution. Enums use `enums` instead of per-card overrides. Numeric values retain existing presentation. Booleans currently render raw source strings; the new optional `properties[path].booleanValues` maps the existing boolean parser's true/false result to labels, without changing parsing. Missing boolean labels fall back to the legacy raw display. Empty values still use `displayEmpty`. Unknown/ineligible overrides are diagnosed and ignored.

Enum translation follows canonical key/flag resolution and preserves existing order and composition. Resolve each member's translation independently, then join with the existing presentation delimiter. For a nonnumeric token that legacy rendering emits literally because no mapping/lookup exists, use that exact token in the same `enums[path]` map. Validation accepts defined members and observed literal tokens; it reports unobserved targets after canonical data loads. Translation never adds entries to `EnumDef.Values` or `Lookups`, allocates flag bits, or makes previously invisible numeric flags visible. Translate `displayEmpty` without changing emptiness, `displayEmptyFirst`, or the canonical empty-filter lookup key. An empty translation string is deliberate and valid. Missing entries continue fallback; null/non-string leaves are invalid, diagnosed, and treated as absent.

For card names, each locale candidate tries `cards[finalId].name` first, then `cardNames[canonical Card.Name]`, before advancing to the next locale. This avoids repeating a name for every printing and does not guess source IDs by stripping suffixes. Front/back faces use their own canonical names; different cards with the same name share this optional fallback, with exact-ID overrides available for exceptions. Name-mirroring properties use this same resolution. Property text and artwork remain per-final-ID targets.

For a default set whose code equals `SetCodeDefault`, resolve only `game.setNameDefault` through the locale chain, then the legacy `SetNameDefault`. Ignore and diagnose `sets[SetCodeDefault].name`; it cannot outrank the authoritative default-set target, even in a more specific locale. Other sets use `sets[code].name` normally.

An empty canonical `gamePlayDeckName` retains its sentinel behavior: show the resolved deck name. A translation cannot turn it into a fixed stack label. User-renamed decks lose the authored-name display binding. Duplicate canonical deck/zone/group names share a catalog target, consistent with today's identity model; deck/stack instances additionally require verified provenance (section 8). A matching name alone never establishes that binding.

Alternative: global source-text replacement cannot disambiguate identical strings used for different entities. Mandatory new deck/zone IDs would require migrations.

### 3. Deterministic per-leaf fallback

The library resolver receives an explicit UI locale, manifest, and available catalogs; it does not consult process culture or Unity globals. Match tags case-insensitively. CGS normalizes its UI aliases to canonical tags before calling the resolver. Reject manifest keys that collide case-insensitively.

Build a deduplicated chain of the selected tag and progressively less specific parents, then `defaultLocale` and its parents. Truncation removes the rightmost subtag and any resulting trailing singleton so extension sequences never become malformed candidates. Only declared catalogs are used. Do not infer siblings/scripts (`pt` does not select `pt-BR`; `zh-Hant` does not select `zh-Hans`). Resolve each leaf from the first valid available translation, then the exact legacy display result. For `es-MX` with default `ja`: `es-MX`, `es`, `ja`, legacy. An invalid/missing UI locale starts at the game default. Absent metadata uses only legacy behavior.

Property display precedence: empty canonical value → localized empty label or legacy empty result; enum → localized resolved members/literal tokens; boolean → the new true/false label map or legacy raw display; eligible nonempty text → per-card override or legacy result; other type → existing presentation. Card-name and default-set target precedence is defined in section 2 before locale fallback. Source-language catalogs can override display text too, without altering base identity.

CGS currently selects `en`, `es`, `fr`, `de`, `it`, `ja`, `ko`, `pt-BR`, `ru`, or `zh-Hans`. The format/resolver accepts other valid tags for future consumers. Author diagnostics compute reachability from the actual selectable locale set and the game-default fallback chain: for example, `es-ES` is not reached from `es` unless separately reached through the default chain. Warn without rejecting the catalog or adding sibling matching. The regional example demonstrates resolver behavior, not a new UI language option.

### 4. Add a display API; preserve existing APIs

Add Unity-independent catalog models and a resolver in this library. Display operations accept canonical objects/keys and a locale/catalog context. Keep `Name`, `PropertyDef.Display`, `EnumDef.Values`, existing getters including `GetPropertyValueString`, `ImageWebUrl`, and serialization behavior unchanged. Do not make existing getters language-dependent.

CGS calls the display API at rendering boundaries: explorer, viewers, deck lists/editor, filters, menus, boards, zones, extra stacks, and dynamic messages embedding game text. Bind messages to canonical references so active messages can refresh. Controls and property selectors carry canonical paths/keys independently of label text; duplicate translated labels must not collide in dictionaries or reverse lookups. Empty-enum controls retain the original `DisplayEmpty` lookup key/flag while displaying its translation. Validation warns about duplicate labels within one control without rejecting valid translations.

Only interactive text search matches both original and current translated names/text. Add a separate presentation-search adapter; keep `UnityCardGame.FilterCards`, deck import/name matching, and `DeckPlayCard.CardQuery` canonical and locale-independent. Typed UI filters still use canonical keys/flags. Presentation sorting never reorders gameplay state. Regression tests must show that a translated name colliding with another card's canonical name changes neither imported decks nor automatically played cards.

Switching languages cannot rename files or mutate models, reprint groups, IDs, import/export output, URL substitutions, gameplay payloads, or provenance metadata. Participants can use different languages in one session; section 8 defines how canonical authored-label references reach them. CGS-owned message templates continue to use CGS's UI catalog; chat and arbitrary user strings are preserved.

### 5. Optional localized artwork and rulebooks

String catalogs cannot change text inside images. `assets` supports `cardImages` (card ID → URI reference), `cardBacks` (back ID → URI reference), `boards` (board ID → URI reference), and singleton `bannerImageUrl`, `cardBackImageUrl`, `playMatImageUrl`, `rulesUrl`. Image variants use the corresponding existing game image format and do not change dimensions, board geometry, or rules.

References are explicit overrides, not templates; never substitute translated names into legacy URLs. Resolve inline references against the definition origin and external-catalog references against that catalog's origin, retaining bundled/cache mappings as described below. Image loading follows the locale chain; an unavailable/invalid variant tries the next candidate then the existing asset. Cache variants separately and keep existing artwork visible while replacements load. A localized rulebook changes the link CGS opens; its external content/availability remains the author's responsibility. The cache/export contract covers catalogs and images, not mirroring external rulebook websites.

This permits complete authored localization without OCR/image generation; text-only catalogs retain original artwork.

### 6. CGS owns asynchronous loading and refresh

The library owns format/resolution; Unity owns I/O and events. Subscribe to `CgsLocalization.Changed` and use Unity Localization's selected locale. At startup, game change, or language change, resolve from inline/cache data immediately and load needed fallback-chain catalogs asynchronously for the selected game. Installed but unselected games also refresh their names/banners/metadata from inline or cached catalogs, without triggering catalog/asset downloads. Refresh active views on completion; new views use the current locale. Never reload gameplay state or reset selections.

Persist the final absolute HTTP(S) response URL of each successfully installed definition/catalog as its origin, independently of `AutoUpdateUrl`. An update URL can differ from the download origin and cannot overwrite it. Do not reconstruct origins from `CardGame.Id` or its host. For an older installation without recorded origin, use bundled relative resources if present; otherwise relative remote resources are unavailable until a successful definition download establishes an origin. Never guess a host-root base. The companion `cgs-games` proxy supplies `X-CGS-Final-URL` from the final server-validated upstream destination after redirects, never from an upstream-supplied header. Expose it using `Access-Control-Expose-Headers` and retain it with cached responses. CGS accepts it only from its trusted HTTPS proxy endpoint and only as an absolute HTTP(S) URL without credentials; a missing/invalid header or untrusted transport leaves remote origin unavailable. Direct downloads use their final transport URL and ignore this header.

Store versioned consumer metadata in `localization/state.json`, external catalog caches in `localization/catalogs/`, and image variants in `localization/assets/`, all inside the game directory. Use generated digest-based filenames and relative local mappings to canonical source URLs, locales, and targets, rather than URL paths as filesystem paths. Export/import of `.cgs.zip` includes these files and their origins/mappings; verify that import under another directory works offline. Missing metadata degrades to base content. A bundled local definition/catalog resolves paths from its actual package location; a cached remote document retains its recorded remote origin while its cache mapping supplies local copies.

For new localization fields, reject `file:` URIs, absolute filesystem paths, and bundled references whose normalized/decoded path or resolved symlink escapes the game directory. Relative references resolving inside the game directory remain valid. Remote HTTP(S) references resolve as URLs and are stored only in the controlled cache directories.

External catalog caches are keyed by canonical game identity, normalized locale, and resolved source URI. Inline catalogs have no independent persistent cache: read them from the current manifest. Every successful manifest install/reload advances a local generation token and replaces inline bindings, including changes at the same URL. Reset localization metadata before repopulating a staged definition with `PopulateObject`, then publish the successful state atomically; an absent/null localization section disables the feature and invalidates bindings immediately. Failed definition reads retain the last successfully installed state. Missing fields must never preserve stale localization through `PopulateObject`.

Follow `autoUpdate`, including `< 0` meaning no automatic downloads. Keep the last usable catalog for that source on refresh failure; changed sources invalidate affected bindings. Without cache, try the next fallback and then base data. Locale changes must not redownload canonical cards/decks/sets. Rapid language/game/manifest changes discard stale callbacks for display using the generation token. Persisted cache publication has a separate ordering guard; matching a source key alone does not authorize an old response to overwrite its cache.

The cache owner assigns a monotonically increasing request ticket per external cache key when each request starts. Keep this sequence across locale switches and same-URI manifest reloads. After validating a response, acquire the per-key publication lock and compare its ticket with the highest successfully published ticket. Publish only if it is newer; discard older/duplicate completions without changing the stored payload, origin, validators, mappings, or freshness timestamps. Stage the validated payload and metadata, then atomically install the entry and advance its published ticket within that same critical section, so a check/write race cannot roll back a newer result. Readers and package exports consume only published entries. Failed/cancelled/malformed responses do not advance the published ticket or erase usable data; an older successful response may still populate its own cache if no later success has been published, but it must independently pass the display-generation check to refresh a view.

Persist the published ticket with its cache entry and resume the sequence above that ticket when reopening the cache. A cache replacement/import starts a new cache-owner incarnation and invalidates outstanding writes from the prior incarnation. Apply the same ordering and atomic-publication rules to localized image keys (which also include target and resource URI); different keys retain independent publication sequences. Verify newest-first completion across language switches and same-URI manifest reloads, then reopen the cache offline to prove the newer catalog/image survives.

Assets also include target and resource URI in cache identity. Bundled catalogs/assets work offline. Invalid document structure rejects the catalog; invalid leaves/unknown targets are ignored with diagnostics while valid siblings remain usable. Authoring validation is strict. No translation failure prevents loading/playing the base game.

### 7. Generated schemas and compatibility checks

Extend schema generation for `cgs.json` and `GameLocalization.json`, URI references, and exactly-one inline/URL constraints. Tags, normalized duplicates, and reference existence also require semantic validation; JSON Schema cannot establish IDs in external card data.

Keep existing required fields/string shapes unchanged and omit unset localization on serialization. Regression fixtures cover original English, Spanish, and Japanese definitions, custom identifiers, all deck formats, nested/lists, flags, back faces, and asset templates. Compare parsed values/output before and after. Unchanged fixtures must still pass the old schema; localized manifests are expected to fail old strict schemas. Unknown-member-tolerant readers can continue using base data, but acceptance by every historical consumer is not promised.

Older CGS editors can deserialize a localized game while silently dropping unknown localization fields on save. Author guidance must state that reading compatibility does not imply editing round-trip preservation; keep the source manifest or use a localization-aware editor. URI-format validation must actually enable a URI checker; parsing JSON alone does not validate the base example.

### 8. Record authored provenance separately from deck contents

Choose explicit provenance rather than inferring authorship from a matching name or card list. CGS records a versioned entry in `localization/provenance.json` when installing an authored deck from `deckUrls`, `gameStartDecks`, or `AllDecks.json`. The entry binds a local artifact generation and content fingerprint to its canonical game identity, authored deck-name target, and source descriptor. Existing deck files and names remain unchanged. Legacy/imported files without a valid binding are unproven and retain their saved names; a fresh authored install can establish provenance. Never retroactively classify files merely by matching name/content.

All CGS user creation, replacement, rename, and save-as paths revoke or omit the authored-name binding before updating a file, including replacement with identical cards and name. A changed external file fingerprint invalidates a binding. Existing authored files and user replacements cannot share a stale binding. Game export/import includes valid sidecars; importing only a deck file does not carry provenance. Editing cards without renaming an already bound in-memory deck can preserve its label binding, but saving a user-created variant follows the unbinding rules.

At stack creation, retain a presentation reference separately from the canonical stack name: `kind` (`authoredDeck`, `gamePlayDeckName`, `extraGroup`, or `userText`) and the appropriate canonical target key. Only verified authored instances carry `authoredDeck`. A game-defined fixed deck label or extra-group label can carry its corresponding reference even when the underlying deck is user-created. An explicit user stack rename clears any authored reference. CGS currently has no full-session save/load feature, and this change does not introduce one. If a consumer later supports session persistence, it can include an optional provenance sidecar keyed by the saved document's fingerprint and deterministic entity locators within that exact document, regenerated on each save. That future integration must preserve the session format and canonical names for legacy saves or mismatched sidecars without guessing. Session-sidecar implementation and verification are conditional on that feature; local deck sidecars and live multiplayer provenance remain required now.

Use NGO `CustomMessagingManager` named messages for an optional `game-content-labels-v1` capability, without registering new required RPC fields or network variables. A capable host offers the version and session/game identity on connection or game change only for a localized game; a capable client acknowledges that offer. While acknowledgement is unavailable (including after five seconds), an unsupported version or a mismatched game selects legacy behavior. A client retains the authenticated offer while switching/downloading the host game and can acknowledge later in the same connection/session once the game matches; this upgrades future creations and label snapshots only, never replays earlier legacy creations. A client-side definition reload re-negotiates the same scope and re-probes labels. The installed NGO 2.13.3 `InvokeNamedMessage` dispatches only registered names, so peers without the capability handler ignore the offer. Verify this against the supported legacy client builds before release; preserve existing connection payloads and baseline compatibility/version checks.

For an authored-label stack creation between negotiated peers, use a new versioned named creation request containing the unchanged canonical arguments of `CreateCardStackServerRpc`, the label reference, and an idempotency request ID scoped to the connection/session. The host applies the same ownership/argument checks and existing stack-creation routine exactly once, and binds provenance directly to the resulting stack instance. This avoids guessing which of two identically named spawned stacks an independent label message belongs to. Do not also send the legacy creation RPC; retransmission reuses the request ID, and no timeout may retry that creation through the legacy path. Unproven/user-text creations and peers without negotiated support retain the legacy request path. This is an additive optional network extension, not a claim of zero new message shapes; existing RPC signatures, fields, and canonical gameplay outcomes stay unchanged.

The authoritative host validates references against the current game and stack ownership, records a revision per stack incarnation, and relays snapshots/updates only to supporting peers. References include the canonical game identity, live stack identity/incarnation, label kind/key, and revision, never translated text or a participant's locale. Late join/reconnect receives a snapshot; a delayed pre-rename/pre-despawn update cannot restore an obsolete binding. Unknown keys/kinds or unsupported versions fall back to the canonical stack name. Existing gameplay messages remain the source of gameplay state.

A legacy host or unsupported sender supplies no reliable provenance: all recipients keep the canonical shared-stack name, including modern recipients. With a capable host/sender, capable recipients resolve the reference locally while legacy recipients ignore the capability offer, receive no provenance payload, and retain existing gameplay replication and canonical names. This is a fallback for unavailable provenance, not name-based translation. Unlocalized games emit no provenance extension and preserve existing behavior. Locale switches change neither creation arguments nor provenance payloads. This does not extend connectivity to CGS versions that were already incompatible.

### 9. Keep generated workflow skills reproducible

The checked-in `.agents/skills` are unmodified OpenSpec 1.16.0 output and remain in this PR as the requested workflow setup. CodeRabbit identified upstream template limitations involving alternate checkbox markers, custom spec-artifact IDs, glob completeness, and resume scaffolding. This change uses dash checkboxes, the standard `spec-driven` schema, both required spec files, and the update workflow for revisions. Verify capability-file completeness independently of CLI status. Do not silently patch generated templates here; regeneration would discard those patches. Track the limitations as upstream follow-up, not completed feature tasks or evidence that the proposal is implemented.

## Risks / Trade-offs

- Missed UI call sites → Inventory CGS surfaces; test active dialogs/messages as well as newly opened views. Schema delivery alone is not completion.
- Authors rename canonical keys → Export actual targets and report orphans; never rebind using translated text. Canonical-name fallback reduces repeated reprint translations without rewriting IDs.
- Partial catalogs mix languages → Per-leaf fallback keeps games playable; coverage reports identify untranslated work.
- Artwork adds download/cache size → Optional, lazy loading with existing cache policies.
- Stale callbacks/mixed-language sessions → Scope cache and refresh identities to game/locale/source/manifest generation; keep translations out of shared state. Provenance-free peers display canonical stack names.
- Additional provenance transport/storage → Test legacy peers, local deck overwrites/saves, and late join before release; test session sidecars only if a future session save/load feature is implemented; keep optional metadata separate from established payloads/files.
- New scripts need glyph/layout support → Reuse CGS font fallbacks and verify multiline text, long labels, and supported scripts downstream.

## Migration Plan

1. Implement/test additive library models, schemas, resolver, and authoring docs here.
2. Adopt the package in CGS and implement/verify the consumer contract. Track that work in a corresponding CGS change referencing this contract.
3. Validate opt-in examples, missing languages, offline export/import, provenance lifecycle, and mixed-language/mixed-capability multiplayer before release.
4. Release CGS support before advertising localized manifests. Existing games require no edits. Removing `localization` disables presentation bindings on the next successful reload without changing source data, existing save/deck formats, or canonical assets. Optional cache/provenance sidecars can remain inert; they cannot enable localization without a manifest. Old strict-validator distributions require the original manifest, and old editors may strip new metadata on save.

## Example Provenance

The user-provided [Suma o Muere](https://cgs.games/atoky/suma_o_muere) and [Hokuto no Ken](https://cgs.games/themadness/%E5%8C%97%E6%96%97%E3%81%AE%E6%8B%B3%E4%B8%96%E7%B4%80%E6%9C%AB%E3%82%AB%E3%83%BC%E3%83%89%E3%83%90%E3%83%88%E3%83%AB%E4%BC%9D%E8%AA%AC_hokuto_no_ken_card_battle) motivate non-English base-language coverage. Web fetch could not access their pages during drafting; no assumptions about their exact manifests are needed. The [examples](examples/README.md) are synthetic, not edits to those games.

### Shipped default-game catalogs and upgrade behavior

Standard Playing Cards, Mahjong, and Dominoes are first-party authored content and ship complete inline catalogs for the ten UI locales. Keep website manifests, StreamingAssets manifests, and Resources fallback packages synchronized from one maintained authoring table. Resources packages include canonical authored deck payloads so WebGL and older installations can use the feature before website deployment and without translation downloads. This is authored content, not runtime machine translation.

The exact shipped name plus canonical update URL identifies the three fallback packages. On staged load, absent metadata receives the bundled catalog; any explicit localization member, including null, wins. This narrowly scoped default-game upgrade is the exception to absent-metadata rollback; custom games keep the existing contract. Optional package failures cannot prevent base-game loading.

Fresh StreamingAssets decks include portable installation sidecars. Never infer legacy/user deck provenance from matching names or content. Where no valid authored artifact exists, install a separate canonical copy under `localization/default-decks/` with a `defaults/<filename>` sidecar key. Lists and startup deck loading use the proven artifact; existing user files retain their names and bytes. Managed copies are read-only originals; editing/saving creates a user deck through the existing path, which revokes any user-path binding. Locale switches do not reinstall decks or change generations. Explicit null hides the managed authored choices and returns to canonical behavior.
