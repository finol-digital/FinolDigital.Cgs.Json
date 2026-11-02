## Purpose

Let game authors supply optional translations and localized presentation resources while preserving every existing game's canonical data and behavior.

## ADDED Requirements

### Requirement: Localization is an additive opt-in
The game specification SHALL accept an optional `localization` object without changing existing required fields or types. When omitted, consumers SHALL preserve legacy parsing, presentation, identifiers, defaults, and serialization behavior and SHALL NOT request localization resources.

#### Scenario: Existing games need no migration
- **WHEN** existing English, Spanish, and Japanese game definitions without localization are loaded under any UI locale
- **THEN** their game-authored strings and canonical data remain unchanged
- **AND** their deck exports, asset URL substitutions, and saved identities match the legacy results
- **AND** serializing a game without localization does not introduce localization metadata

### Requirement: Declare source locale and catalog sources
A localization manifest SHALL require `defaultLocale`, describing the base content's language, and accept an optional `locales` map. Locale tags SHALL be valid BCP 47 tags and unique case-insensitively. Each locale source SHALL contain exactly one nonempty `url` URI reference or `inline` catalog object. No English or default-locale catalog SHALL be required.

#### Scenario: External and inline sources coexist
- **WHEN** a game declares default `ja`, an external `es` catalog, and an inline `fr` catalog
- **THEN** the manifest is valid without an English or Japanese catalog
- **AND** an absent or empty `locales` map is also valid

#### Scenario: Ambiguous or malformed sources are rejected by validation
- **WHEN** a manifest declares both `url` and `inline` for one locale, neither, invalid tags, or both `es` and `ES`
- **THEN** authoring validation reports the corresponding error
- **AND** runtime consumers fall back to base game content if the manifest cannot be used

### Requirement: Resolve translations per leaf
Consumers SHALL resolve each leaf through the UI locale and its parents, then the default locale and its parents, then the legacy display result. Matching SHALL be case-insensitive and deduplicated, using only declared catalogs. Parent truncation SHALL remove trailing singleton subtags. Consumers SHALL NOT choose undeclared sibling/script variants or assume English fallback.

#### Scenario: Partial regional translation with non-English base
- **GIVEN** UI locale `es-MX`, default `ja`, and catalogs for `es-MX`, `es`, and `ja`
- **WHEN** the regional catalog supplies a card name, the parent supplies its rules, and neither supplies its set name
- **THEN** the card name uses `es-MX`, rules use `es`, and set name uses `ja` if present, otherwise the base value

#### Scenario: Unsupported locale and deliberate empty translation
- **WHEN** the UI selects `pt` but only `pt-BR` is translated and the base locale is `ja`
- **THEN** consumers use the Japanese/default fallback, not `pt-BR`
- **AND** a valid empty string in a selected catalog is returned as empty rather than treated as missing

#### Scenario: Invalid UI locale and extended tags
- **WHEN** the UI locale is absent or invalid
- **THEN** resolution starts at the declared game default
- **WHEN** a valid locale contains extension subtags
- **THEN** parent lookup removes subtags without producing a candidate ending in a singleton

### Requirement: Catalogs address canonical entities
Catalogs SHALL provide `game` display fields (`name`, `copyright`, `gamePlayDeckName`, `setNameDefault`); name objects in `cards`, `sets`, `decks`, `boards`, and `cardBacks`; and string maps `zones` and `extraGroups`. Keys SHALL be final card IDs, set codes, authored deck/zone/group names, and board/back IDs respectively, preserved exactly and matched case-sensitively.

#### Scenario: Source integrations and faces retain identity
- **WHEN** cards arrive through custom identifiers, nested sets, per-set downloads, or generated back-face/reprint records
- **THEN** catalog targets refer to their final canonical IDs, independent of the input JSON layout
- **AND** changing a translated name does not change those IDs

#### Scenario: Named groups and deck-name sentinel
- **WHEN** multiple zones share a canonical name and a catalog translates it
- **THEN** all those zones share the translated label
- **WHEN** the canonical `gamePlayDeckName` is empty
- **THEN** the stack uses the resolved deck name even if a catalog supplies `gamePlayDeckName`

### Requirement: Translate property labels and textual values
Catalogs SHALL support `properties[path].display`, `properties[path].displayEmpty`, and `cards[id].properties[path]` final display strings for string, escaped-string, string-list, and textual nested properties. Paths SHALL use existing parsed lookup keys. Text overrides SHALL NOT change typed values, object/list structure, or source escape processing. Name-mirroring properties SHALL use the resolved card name.

#### Scenario: Nested and list content is display-only
- **WHEN** a catalog translates a nested label, a textual list aggregate, and an indexed textual child using their canonical lookup paths
- **THEN** each targeted display uses its translation while original values and list order remain intact
- **AND** an aggregate override does not implicitly rewrite individually rendered children
- **AND** translations are not decoded again as escaped source data

#### Scenario: Empty and typed properties
- **WHEN** a property is empty and has a translated `displayEmpty`
- **THEN** the empty label is used and the property remains empty
- **AND** `displayEmptyFirst` retains its original behavior
- **WHEN** a catalog attempts a per-card text override of an enum, integer, or boolean property
- **THEN** that override is diagnosed and ignored; enums use their member labels and other types retain typed presentation

### Requirement: Translate enum labels without changing members
Catalogs SHALL support `enums[propertyPath][canonicalKey]` labels for string/object enums, enum lists, and hexadecimal flags. Consumers SHALL resolve canonical members first and translate each display label independently, preserving keys, flag values, order, and existing composition. Missing labels SHALL follow per-leaf fallback.

#### Scenario: Mixed translated flag labels
- **GIVEN** flags `0x1` and `0x2` with base labels `Fire` and `Water`
- **WHEN** a card contains both flags and only `0x1` is translated to `Fuego`
- **THEN** its display is `Fuego | Water`
- **AND** filtering, bit operations, and serialized values remain identical to the untranslated game

### Requirement: Offer localized resource references
Catalogs SHALL accept optional `assets.cardImages`, `assets.cardBacks`, and `assets.boards` URI-reference maps keyed by canonical IDs, plus `bannerImageUrl`, `cardBackImageUrl`, `playMatImageUrl`, and `rulesUrl` within `assets`. These SHALL be explicit presentation references, not translated URL templates. Image variants SHALL retain the existing image format and gameplay geometry.

#### Scenario: Localized card artwork and rulebook
- **WHEN** Spanish content supplies an image reference for a card and a rulebook reference
- **THEN** the consumer can display that artwork and open that rulebook in Spanish
- **AND** the card's canonical image URL, ID, dimensions, and behavior remain unchanged
- **AND** a catalog without asset entries continues using existing assets

### Requirement: Resolve relative references from their containing resource
Relative catalog URLs SHALL resolve from the game definition's origin. Relative inline asset references SHALL resolve from the definition's origin; external-catalog asset references SHALL resolve from that catalog's origin. Local bundled references SHALL resolve within the game directory. Consumers SHALL retain remote origins after caching and treat unresolvable references as unavailable.

#### Scenario: Downloaded catalog references adjacent artwork
- **WHEN** a definition at `https://example.invalid/game/cgs.json` references `locales/es.json`, which references `images/001.png`
- **THEN** the artwork resolves to `https://example.invalid/game/locales/images/001.png`, including after a restart using cached data

### Requirement: Validation does not compromise base-game availability
Authoring tools SHALL validate structure, locale tags, source exclusivity, translation types, and target references and expose missing-translation coverage. Runtime consumers SHALL ignore invalid leaves/unknown targets with diagnostics, reject unusable catalog structure, and continue fallback. Null SHALL be invalid; an empty catalog SHALL be valid. Targets SHALL never be created or modified by translations.

#### Scenario: A partially invalid translation remains safe
- **WHEN** a catalog contains a valid card name, a null property label, and an unknown card ID
- **THEN** validation reports the invalid entries
- **AND** runtime consumers can use the valid name, fall back for the label, and ignore the unknown ID
- **AND** gameplay remains available

### Requirement: Canonical APIs remain locale-independent
Existing canonical fields, getters, serialization, deck imports/exports, URL-template substitution, and comparison/reprint behavior SHALL remain independent of locale. New presentation operations SHALL accept an explicit locale context and SHALL NOT mutate canonical models. Updated schemas SHALL continue accepting existing valid definitions; older strict schemas need not accept new localization fields.

#### Scenario: Switching languages preserves portable data
- **WHEN** a game is rendered in two locales and exported through every existing deck format
- **THEN** exports, canonical values, lookup flags, reprint groups, file identities, and legacy URL substitutions are unchanged
- **AND** old definitions validate against both old and updated schemas
