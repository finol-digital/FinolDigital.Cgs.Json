# Tasks

This is an unimplemented proposal. Sections 1–3 belong to this repository; sections 4–5 require the separate Card-Game-Simulator repository. Keep downstream work visible until the user-facing feature is verified.

## 1. Add the format and schemas

- [ ] 1.1 Add nullable localization metadata and typed catalog/source models, preserving canonical constructors/getters and exact dictionary keys; verify legacy deserialize/serialize fixtures and mixed-case ID round trips.
- [ ] 1.2 Extend schema generation with `GameLocalization.json` and the optional manifest shape, including URI references and exclusive URL/inline sources; regenerate schemas and verify positive/negative fixtures for old and new definitions.
- [ ] 1.3 Implement semantic diagnostics for tags, normalized duplicates, invalid leaves, ineligible property overrides, and unknown targets; verify malformed entries cannot alter or prevent loading base data.
- [ ] 1.4 Document every catalog member, reference origin, target key convention, empty/sentinel behavior, and old-schema compatibility boundary in author guidance linked from README; validate all documented JSON examples.

## 2. Add locale-independent presentation resolution

- [ ] 2.1 Implement explicit-locale, per-leaf resolution; verify exact/parent/default/base fallback, case matching, invalid UI locales, extension truncation, deliberate empty strings, and no sibling/script guessing.
- [ ] 2.2 Implement display resolution for names, nested/list text, labels, empty labels, enum keys/flags, zones, groups, boards, and backs; verify missing entries and typed values preserve canonical semantics.
- [ ] 2.3 Implement localized resource candidate resolution with containing-document origins; verify inline/external/local reference cases and unchanged legacy image-template results.
- [ ] 2.4 Add canonical target enumeration and coverage reporting; verify generated face/reprint IDs, custom source identifiers, flattened/indexed paths, and orphan translations are exposed without guessing source layout.
- [ ] 2.5 Document the display API and integration boundaries; verify examples resolve using explicit locale contexts without Unity dependencies or mutation of existing objects.

## 3. Verify library backwards compatibility

- [ ] 3.1 Add English-, Spanish-, and Japanese-base fixtures with and without localization; verify legacy parsing, original display output, no localization requests, and omission of absent metadata.
- [ ] 3.2 Cover every supported deck export format, reprint grouping, enum flags, extras, custom identifiers, back faces, and URL substitutions; verify outputs/identities remain identical across selected locales.
- [ ] 3.3 Convert proposal examples into maintained author fixtures when the schema is implemented; verify original definitions against old and new schemas and localized definitions against the new schema.

## 4. Integrate in CGS (downstream repository)

- [ ] 4.1 Create the corresponding CGS change and adopt the updated package; deliver a display-surface inventory and verify canonical data flows remain separate from UI bindings.
- [ ] 4.2 Connect the existing locale event/selected locale to catalog loading and refresh; verify inline, external, bundled, cached, `autoUpdate < 0`, missing, malformed, and rapid-switch cases in automated tests.
- [ ] 4.3 Add localized asset loading and separate caches; verify regional failures fall back to parent/base assets without overwriting originals, and rulebook links use the selected reference.
- [ ] 4.4 Bind game/card/set/deck names, property/enum/empty labels, card text, metadata, groups, zones, boards, backs, and dynamic messages; verify active and newly opened screens refresh without losing selection or edits.
- [ ] 4.5 Update text search and translated filter options; verify both canonical and translated queries match while selected canonical enum flags remain unchanged across switches.
- [ ] 4.6 Preserve authored deck bindings only for authored instances, clearing on rename; verify user-created/renamed decks and chat remain unchanged even when names collide with catalog keys.
- [ ] 4.7 Add downstream author/runtime guidance and diagnostics; verify documented installation, offline packaging, and rollback steps work with the sample catalogs.

## 5. Verify the complete integration

- [ ] 5.1 Exercise original English/Spanish/Japanese games, complete/partial catalogs, startup selection, repeated language switches, long labels, multiline rules, supported fonts, and offline use; record results against the surface inventory on desktop and mobile layouts.
- [ ] 5.2 Verify two participants with different UI languages can load/play/save the same deck/session with identical canonical state and network behavior, including language changes mid-session.
- [ ] 5.3 Verify feature rollback by removing localization metadata, and confirm unchanged old games require no edits and retain original files/exports/assets; record release dependency on the CGS consumer implementation.
