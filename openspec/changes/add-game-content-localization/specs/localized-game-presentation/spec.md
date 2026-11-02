## Purpose

Connect authored game translations to the existing CGS language setting while keeping gameplay, saved content, and multiplayer state independent of each player's language.

## ADDED Requirements

### Requirement: Game presentation follows the UI language
CGS consumers SHALL use the current UI locale for all supported game-authored display targets. They SHALL refresh active and newly opened views after language changes and after relevant catalogs load, without restarting or reloading gameplay. Game-authored content SHALL use the game's fallback contract rather than the application catalog's English fallback.

#### Scenario: Switch languages with game UI open
- **GIVEN** complete Spanish translations for a loaded game
- **WHEN** the user switches the UI to Spanish with a card viewer, deck list, and filter controls open
- **THEN** game/card/deck/set names, rules text, property labels, enum options, empty labels, group/zone/board/back labels, and game metadata use the translated targets wherever rendered
- **AND** messages embedding those targets refresh while application-owned message templates use the UI translation catalog
- **AND** selections, unsaved deck edits, card positions, and gameplay state remain intact

#### Scenario: New views and original-language games
- **WHEN** the user opens another game-content view after changing language
- **THEN** it resolves the current locale immediately from available data
- **AND** an untranslated Spanish or Japanese game continues showing its original authored content without assuming English

### Requirement: Localized search and controls retain canonical meaning
For localized games, text search SHALL match both canonical and current translated names/text. Filter options SHALL show translated labels but retain canonical property keys, enum values/flags, and selected values. Any presentation sorting SHALL NOT change gameplay order, and games without localization SHALL retain their existing search behavior.

#### Scenario: Search by translated and original name
- **GIVEN** canonical card `Flame` displayed as `Llama`
- **WHEN** a user searches for either name in Spanish
- **THEN** both searches can find the same canonical card
- **AND** cards sharing a translated name remain distinct by canonical ID

#### Scenario: Active enum filter survives a language switch
- **WHEN** a selected `0x1` enum option changes its visible label from `Fire` to `Fuego`
- **THEN** the selection remains `0x1` and the same cards satisfy the filter

### Requirement: Catalog loading is resilient and isolated
Consumers SHALL load only needed locale-chain catalogs and keep usable cached versions scoped to canonical game identity, locale, and source. Loading SHALL honor the existing update policy, retain last-good data on refresh failure, and fall back when unavailable. A language switch SHALL NOT redownload canonical game datasets or allow stale asynchronous results to override the current game's locale.

#### Scenario: Offline and failed catalog refresh
- **WHEN** a user selects a language while offline
- **THEN** bundled/cached catalogs are used if available and missing entries follow the normal fallback chain
- **AND** a failed or malformed refresh does not replace a usable cached catalog or block gameplay

#### Scenario: Automatic downloads are disabled
- **WHEN** a game has `autoUpdate < 0` and the user switches languages
- **THEN** available inline/local/cached catalogs are used without automatic network requests
- **AND** unavailable translations fall back to base content

#### Scenario: Rapid selection and game changes
- **WHEN** a user switches Spanish to Japanese or changes games before the Spanish request completes
- **THEN** its completion cannot replace the current presentation
- **AND** changing a manifest's source prevents unrelated old-source cache data from being reused

### Requirement: Localized assets preserve availability
Consumers SHALL use localized artwork when supplied and available, with locale-chain fallback followed by the original asset. They SHALL isolate localized caches from canonical assets, preserve usable artwork during loading, and keep IDs, file identities, and geometry unchanged. The selected rulebook reference SHALL follow the same locale selection contract without rewriting canonical metadata.

#### Scenario: Missing regional artwork
- **WHEN** an `es-MX` card image fails to load but an `es` image is available
- **THEN** the Spanish image can be displayed
- **AND** if all localized variants fail, the existing card image is used
- **AND** no failed variant overwrites the canonical asset cache

### Requirement: Presentation is local to each participant
Localization SHALL NOT alter network messages, game/card/deck identities, shared state, saved game/deck content, or imported/exported data. Each participant SHALL resolve shared canonical content using their own selected UI language. User-authored names, edits, and chat SHALL remain as entered.

#### Scenario: Two languages in one session
- **WHEN** Spanish and Japanese players inspect the same shared card, deck, and play zone
- **THEN** each sees their own available translations while both reference the same canonical entities
- **AND** switching either player's language does not change the other player's display or emit gameplay mutations

#### Scenario: User renames an authored deck
- **WHEN** a user replaces an authored deck name with their own text and changes language
- **THEN** their chosen text remains visible and saved as entered
- **AND** a user-created deck with a matching authored name is not automatically treated as that authored deck

### Requirement: Translated content remains readable
Consumers SHALL render game text through supported locale font/layout handling, preserving authored Unicode and line breaks under the normal UI text rules. Long translated labels and non-Latin text SHALL remain accessible in supported views and controls.

#### Scenario: Non-Latin and multiline content
- **WHEN** supported Japanese content and longer Spanish labels are shown in card viewers, deck lists, filters, and game dialogs
- **THEN** glyphs and authored line breaks render correctly and controls keep their normal wrapping, scrolling, or other access to the complete text
