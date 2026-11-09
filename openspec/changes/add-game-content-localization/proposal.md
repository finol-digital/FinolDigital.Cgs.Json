# Proposal: Localize authored game content

## Why

CGS can now change its interface language, but game-authored names, rules text, property labels, and enum labels remain in the author's language. Players need the same language selection to apply to game content without changing the identity or behavior of existing games, cards, decks, and saved deck files.

## What Changes

- Add an optional `localization` section to `cgs.json`, declaring the original content's `defaultLocale` and locale catalogs supplied inline or through separate JSON files.
- Define additive translations for game and card names, card text, set and deck names, property labels, boolean/empty-value labels, enum labels (including unmapped source tokens), extra-deck groups, boards, and play zones. Per-card overrides and a canonical-name fallback cover reprints without changing their IDs.
- Resolve each display value using the selected UI locale, its parents, the game's declared default locale and its parents, then the existing authored value. No game-content fallback implicitly assumes English; incomplete translations remain playable.
- Provide optional localized image and rulebook references for authored content whose text is embedded in assets. Translations and alternate artwork must be supplied by authors; CGS does not automatically translate content.
- Specify the downstream CGS integration: live language changes, interactive translated search separated from canonical gameplay/import queries, stable control keys, portable offline caches, and independent presentation for multiplayer participants.
- Add optional local authored-deck provenance and negotiated, versioned stack-creation/label messages for shared stacks. Preserve user-created/renamed names; never infer provenance from matching names or cards. Peers without provenance support use canonical stack names and the existing creation path.
- Preserve existing fields, string types, parsing, IDs, enum values/flags, image-template substitutions, deck file formats, and gameplay payloads. The optional metadata extension carries canonical label references, never translations. Games without localization metadata require no migration and behave exactly as before.

## Capabilities

### New Capabilities

- `game-localization-format`: Optional catalog format, translation targets, validation, fallback, and compatibility guarantees in the game specification.
- `localized-game-presentation`: Consumer contract connecting catalogs to CGS language selection, rendering, search/filter controls, resource loading, and multiplayer presentation.

### Modified Capabilities

None. This repository has no existing OpenSpec capabilities. The separate CGS `localized-interface` capability remains responsible for application-owned text; this change supplies the game-content contract it previously excluded.

## Impact

- **This repository:** additive models on `CardGame`, locale/catalog DTOs and a Unity-independent resolver; generated `schema/cgs.json` and a new `schema/GameLocalization.json`; authoring examples, README guidance, and focused compatibility tests. Existing `AllCards.json`, `AllDecks.json`, and `AllSets.json` data shapes remain valid.
- **Card-Game-Simulator repository:** package adoption, persisted download origins and portable caches, authored-deck provenance sidecars, optional presentation-metadata negotiation, canonical-to-display bindings across all game-content UI, and integration tests. The schema update alone cannot deliver the user-facing feature.
- **cgs-games repository:** expose the server-validated final upstream document URL through a CORS-readable proxy response header for WebGL relative resources.
- **Session persistence:** CGS has no full-session save/load feature. This change does not add one; optional session provenance sidecars are conditional on a future feature. Deck and multiplayer provenance remain in scope.
- **Authors:** opt-in translations, with no changes required to existing source-language data or external card APIs. User-authored deck names, chat, and other arbitrary user input are preserved.
- **Compatibility boundary:** old definitions work in new consumers. Old strict schemas can reject new optional fields, and old CGS editors can discard localization metadata when saving. Existing gameplay protocol contracts stay unchanged; mixed-capability sessions fall back to canonical shared-stack names where provenance is unavailable. This does not promise connectivity between previously incompatible CGS versions.

This change is a proposal only. Implementation and downstream rollout are tracked in [tasks.md](tasks.md).

Built-in content scope: ship complete offline catalogs for all ten UI languages for Standard Playing Cards, Mahjong, and Dominoes, including safe upgrades of existing installs and separately proven authored decks without modifying user files.
