## Purpose

Let game authors supply optional translations and localized presentation resources while preserving every existing game's canonical data and behavior.

## ADDED Requirements

### Requirement: Localization is an additive opt-in
The game specification SHALL accept optional `localization` without changing existing required fields or types. When omitted, the library and custom-game consumers SHALL preserve legacy parsing, presentation, identifiers, defaults, and serialization without localization requests. CGS MAY supply shipped catalogs for the three recognized default games under the presentation requirements.

#### Scenario: Existing games need no migration
- **WHEN** existing custom English, Spanish, and Japanese game definitions without localization are loaded under any UI locale
- **THEN** their game-authored strings and canonical data remain unchanged
- **AND** their deck exports, asset URL substitutions, and saved identities match the legacy results
- **AND** serializing a game without localization does not introduce localization metadata


#### Scenario: Explicit null and absent metadata remain distinct when saved
- **WHEN** a game definition with `localization: null` is loaded and saved through `CardGame`
- **THEN** null SHALL remain explicit, while absent metadata SHALL remain omitted
- **AND** direct assignment of null to `Localization` SHALL clear the member; callers requesting explicit null SHALL set `LocalizationSpecified` afterward

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
- **THEN** that override is diagnosed and ignored; enums use their member/token labels, booleans use their optional label map, and integers retain existing presentation

### Requirement: Translate enum labels without changing members
Catalogs SHALL support `enums[propertyPath][canonicalToken]` labels for defined members and observed unmapped nonnumeric tokens displayed literally by legacy rendering. Consumers SHALL resolve canonical members first, translate labels independently, and preserve flags, order, and composition. Translations SHALL NOT register new members or allocate lookup bits. Missing labels SHALL follow per-leaf fallback.

#### Scenario: Mixed translated flag labels
- **GIVEN** flags `0x1` and `0x2` with base labels `Fire` and `Water`
- **WHEN** a card contains both flags and only `0x1` is translated to `Fuego`
- **THEN** its display is `Fuego | Water`
- **AND** filtering, bit operations, and serialized values remain identical to the untranslated game

#### Scenario: Unmapped source token can be translated
- **GIVEN** an enum property contains literal `wind`, absent from the authored enum mapping and existing lookup table
- **WHEN** a Spanish catalog supplies `enums.element.wind` as `Viento`
- **THEN** the displayed token becomes `Viento` without registering `wind` as a member or changing any lookup flags
- **AND** unobserved catalog tokens are diagnosed once canonical data has loaded

### Requirement: Share card-name translations across reprints
Catalogs SHALL support `cardNames[canonicalName]` as a name-only fallback. Within each locale candidate, consumers SHALL try `cards[finalId].name` then `cardNames[canonicalName]` before advancing to the next locale. Faces SHALL use their own canonical names. Consumers SHALL NOT infer source IDs by removing suffixes, change reprint groups, or apply name fallback to card text/assets.

#### Scenario: Reprints and exact overrides
- **GIVEN** two printings share canonical name `Flame` but have different final IDs, and Spanish `cardNames.Flame` is `Llama`
- **WHEN** both are displayed in Spanish
- **THEN** both show `Llama` regardless of which printing was parsed first
- **WHEN** one final ID also has a Spanish name override
- **THEN** that printing uses its override and the other keeps `Llama`
- **AND** a back face named `Ash` does not inherit the translation for `Flame`

### Requirement: Translate boolean display values
For boolean properties, catalogs SHALL accept optional `properties[path].booleanValues.true` and `.false` strings. Consumers SHALL select labels using existing canonical boolean parsing and normal locale fallback. Empty values SHALL use the empty-label contract; absent boolean labels SHALL retain legacy raw presentation. Translation SHALL NOT change parsed truth values or filtering.

#### Scenario: Different true spellings share a label
- **WHEN** boolean source values `yes`, `1`, and `true` are displayed with Spanish true label `Sí`
- **THEN** all display `Sí` while their original strings and parsed values remain unchanged
- **AND** an empty value uses `displayEmpty` rather than the false label
- **AND** a missing false label preserves the original display of `no`

### Requirement: Default set names have one authoritative target
For the set identified by `setCodeDefault`, consumers SHALL resolve `game.setNameDefault` through the locale chain then use the legacy default-set name. They SHALL ignore and diagnose `sets[setCodeDefault].name`, even in a more specific locale. Other sets SHALL use their ordinary set-name targets.

#### Scenario: Competing default-set translations
- **WHEN** `es-MX` supplies only a set-name entry for the default set and `es` supplies `game.setNameDefault`
- **THEN** the Spanish game-default label is displayed and the conflicting regional entry is diagnosed
- **AND** if the authoritative target is missing throughout the chain, the legacy default-set name is used

### Requirement: Offer localized resource references
Catalogs SHALL accept optional `assets.cardImages`, `assets.cardBacks`, and `assets.boards` URI-reference maps keyed by canonical IDs, plus `bannerImageUrl`, `cardBackImageUrl`, `playMatImageUrl`, and `rulesUrl` within `assets`. These SHALL be explicit presentation references, not translated URL templates. Image variants SHALL retain the existing image format and gameplay geometry.

#### Scenario: Localized card artwork and rulebook
- **WHEN** Spanish content supplies an image reference for a card and a rulebook reference
- **THEN** the consumer can display that artwork and open that rulebook in Spanish
- **AND** the card's canonical image URL, ID, dimensions, and behavior remain unchanged
- **AND** a catalog without asset entries continues using existing assets

### Requirement: Resolve relative references from their containing resource
Relative catalog/inline asset references SHALL resolve from the definition origin; external-catalog assets SHALL resolve from that catalog's origin. Remote origins SHALL be the persisted final download URLs, independent of update URLs or host-derived IDs. Bundled documents SHALL resolve from their package locations within the game directory. Missing origins SHALL NOT be guessed. Existing URL fields SHALL retain their absolute-URI contract.

#### Scenario: Downloaded catalog references adjacent artwork
- **WHEN** a definition at `https://example.invalid/game/cgs.json` references `locales/es.json`, which references `images/001.png`
- **THEN** the artwork resolves to `https://example.invalid/game/locales/images/001.png`, including after a restart using cached data

#### Scenario: Update URL differs from download origin
- **WHEN** a definition downloaded from `https://example.invalid/game/cgs.json` declares a different update URL
- **THEN** its relative catalog reference still resolves against the recorded download origin after restart
- **AND** an old installation without an origin uses available bundled files or falls back, never a guessed host-root URL

### Requirement: Local resource references stay within the game package
New localization references SHALL allow HTTP(S) URLs and relative package references. Consumers SHALL reject `file:` URIs, absolute filesystem paths, and local references whose decoded/normalized or symlink-resolved target escapes the game directory. Remote URLs SHALL NOT determine filesystem paths outside controlled cache storage. Invalid resources SHALL use normal fallback without blocking gameplay.

#### Scenario: Imported catalog attempts to escape its package
- **WHEN** a bundled catalog's asset reference resolves outside its game directory, including through an encoded path or symlink
- **THEN** the consumer rejects that reference and uses fallback content
- **AND** a relative parent reference that resolves inside the game directory remains usable

### Requirement: Validation does not compromise base-game availability
Authoring tools SHALL validate structure, locale tags, source exclusivity, translation types, and target references and expose missing-translation coverage. Runtime consumers SHALL ignore invalid leaves/unknown targets with diagnostics, reject unusable catalog structure, and continue fallback. Null SHALL be invalid; an empty catalog SHALL be valid. Targets SHALL never be created or modified by translations.

#### Scenario: A partially invalid translation remains safe
- **WHEN** a catalog contains a valid card name, a null property label, and an unknown card ID
- **THEN** validation reports the invalid entries
- **AND** runtime consumers can use the valid name, fall back for the label, and ignore the unknown ID
- **AND** gameplay remains available

#### Scenario: Authoring diagnostics preserve valid but ambiguous labels
- **WHEN** a catalog uses duplicate translated property labels within one control or includes a locale unreachable from the consumer's selectable locales and game-default chain
- **THEN** authoring diagnostics warn about the duplicate labels or unreachable catalog without rejecting otherwise valid translations
- **AND** consumers keep distinct canonical property keys and do not infer sibling locales

### Requirement: Canonical APIs remain locale-independent
Existing canonical fields, getters, serialization, deck imports/exports, URL-template substitution, and comparison/reprint behavior SHALL remain independent of locale. New presentation operations SHALL accept an explicit locale context and SHALL NOT mutate canonical models. Updated schemas SHALL continue accepting existing valid definitions; older strict schemas need not accept new localization fields.

#### Scenario: Switching languages preserves portable data
- **WHEN** a game is rendered in two locales and exported through every existing deck format
- **THEN** exports, canonical values, lookup flags, reprint groups, file identities, and legacy URL substitutions are unchanged
- **AND** old definitions validate against both old and updated schemas

#### Scenario: Old editors do not preserve unknown metadata
- **WHEN** authors consult compatibility guidance for an old CGS editor
- **THEN** it explains that reading base content does not guarantee preserving localization on save
- **AND** it directs authors to retain the source manifest or use a localization-aware editor
