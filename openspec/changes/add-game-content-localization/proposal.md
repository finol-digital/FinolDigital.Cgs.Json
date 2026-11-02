# Proposal: Localize authored game content

## Why

CGS can now change its interface language, but game-authored names, rules text, property labels, and enum labels remain in the author's language. Players need the same language selection to apply to game content without changing the identity or behavior of existing games, cards, decks, and saved sessions.

## What Changes

- Add an optional `localization` section to `cgs.json`, declaring the original content's `defaultLocale` and locale catalogs supplied inline or through separate JSON files.
- Define additive translations for game and card names, card text, set and deck names, property labels and empty-value text, enum labels, extra-deck groups, boards, and play zones. Catalog entries address canonical identifiers rather than translated text.
- Resolve each display value using the selected UI locale, its parents, the game's declared default locale and its parents, then the existing authored value. No game-content fallback implicitly assumes English; incomplete translations remain playable.
- Provide optional localized image and rulebook references for authored content whose text is embedded in assets. Translations and alternate artwork must be supplied by authors; CGS does not automatically translate content.
- Specify the downstream CGS integration: live language changes, translated search/display with canonical filter values, offline caches, and independent presentation for multiplayer participants.
- Preserve existing fields, string types, parsing, IDs, enum values/flags, image-template substitutions, deck formats, and network state. Games without localization metadata require no migration and behave exactly as before.

## Capabilities

### New Capabilities

- `game-localization-format`: Optional catalog format, translation targets, validation, fallback, and compatibility guarantees in the game specification.
- `localized-game-presentation`: Consumer contract connecting catalogs to CGS language selection, rendering, search/filter controls, resource loading, and multiplayer presentation.

### Modified Capabilities

None. This repository has no existing OpenSpec capabilities. The separate CGS `localized-interface` capability remains responsible for application-owned text; this change supplies the game-content contract it previously excluded.

## Impact

- **This repository:** additive models on `CardGame`, locale/catalog DTOs and a Unity-independent resolver; generated `schema/cgs.json` and a new `schema/GameLocalization.json`; authoring examples, README guidance, and focused compatibility tests. Existing `AllCards.json`, `AllDecks.json`, and `AllSets.json` data shapes remain valid.
- **Card-Game-Simulator repository:** package adoption, catalog/asset loading and caching, canonical-to-display bindings across all game-content UI, and integration tests. The schema update alone cannot deliver the user-facing feature.
- **Authors:** opt-in translations, with no changes required to existing source-language data or external card APIs. User-authored deck names, chat, and other arbitrary user input are preserved.
- **Compatibility boundary:** old definitions work in new consumers. Old strict schema validators can reject new optional fields; new localized manifests are not promised to validate against an older schema.

This change is a proposal only. Implementation and downstream rollout are tracked in [tasks.md](tasks.md).
