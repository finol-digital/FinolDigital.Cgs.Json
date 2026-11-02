# Tasks

This is an unimplemented proposal. Sections 1–3 belong to this repository; sections 4–5 require the separate Card-Game-Simulator repository. Keep downstream work visible until the user-facing feature is verified.

## 1. Add the format and schemas

- [ ] 1.1 Add nullable localization metadata and typed catalog/source models, including `cardNames` and boolean labels, preserving canonical constructors/getters and exact dictionary keys; verify legacy deserialize/serialize fixtures and mixed-case ID round trips.
- [ ] 1.2 Extend schema generation with `GameLocalization.json` and the optional manifest shape, including URI references and exclusive URL/inline sources; regenerate schemas and verify positive/negative fixtures for old and new definitions.
- [ ] 1.3 Implement semantic diagnostics for tags, normalized duplicates, invalid leaves, ineligible property overrides, duplicate control labels, conflicting default-set targets, and unknown targets; verify observed literal enum tokens are accepted without registering lookups and invalid entries cannot block base data.
- [ ] 1.4 Document every catalog member, reference origin, target key convention, empty/sentinel behavior, and older-schema/editor compatibility boundary in author guidance linked from README; validate documented JSON with active URI-format checking and warn that old editors can discard new metadata.

## 2. Add locale-independent presentation resolution

- [ ] 2.1 Implement explicit-locale, per-leaf resolution; verify exact/parent/default/base fallback, case matching, invalid UI locales, extension truncation, deliberate empty strings, and no sibling/script guessing.
- [ ] 2.2 Implement display resolution for names, nested/list text, boolean/empty labels, defined/literal enum tokens, zones, groups, boards, and backs; verify parsed values/flags stay unchanged and `game.setNameDefault` is authoritative for the default set.
- [ ] 2.3 Implement localized resource candidate resolution using explicit recorded origins/package locations; verify inline/external/local cases, update-URL independence, missing-origin fallback, package path confinement, and unchanged legacy image templates.
- [ ] 2.4 Add canonical target enumeration and coverage reporting; verify generated face/reprint IDs, canonical-name targets, custom identifiers, flattened/indexed paths, and orphan translations are exposed without guessing source layout.
- [ ] 2.5 Document the display API and integration boundaries; verify examples resolve using explicit locale contexts without Unity dependencies or mutation of existing objects.
- [ ] 2.6 Implement exact-ID then canonical-name fallback within each locale candidate; verify reprint load-order changes, face-specific names, shared canonical names, exact overrides, and name-mirroring properties without suffix stripping.

## 3. Verify library backwards compatibility

- [ ] 3.1 Add English-, Spanish-, and Japanese-base fixtures with and without localization; verify legacy parsing, original display output, no localization requests, and omission of absent metadata.
- [ ] 3.2 Cover every supported deck export format, reprint grouping, enum flags/literal tokens, booleans, extras, custom identifiers, back faces, and URL substitutions; verify outputs, parsing, and canonical queries remain identical across locales.
- [ ] 3.3 Convert proposal examples into maintained author fixtures when the schema is implemented; verify original definitions against old/new schemas and localized definitions against the new schema using an active URI-format checker, including a negative relative-legacy-URL case.

## 4. Integrate in CGS (downstream repository)

- [ ] 4.1 Create the corresponding CGS change and adopt the updated package; deliver a display-surface inventory and verify canonical data flows remain separate from UI bindings.
- [ ] 4.2 Connect the locale event to catalog loading and generation-scoped refresh; reset localization before successful definition repopulation and verify inline replacement at the same URL, absent/null removal, failed reads, `autoUpdate < 0`, and rapid switches without restart.
- [ ] 4.3 Persist definition/catalog origins and portable cache mappings under the game directory; verify redirects, differing update URLs, missing origins, package path confinement, and offline export/import under a different directory.
- [ ] 4.4 Add localized image loading with separate caches and rulebook link selection; verify failed regional variants fall back to parent/base assets without overwriting originals and cached images travel with packages.
- [ ] 4.5 Bind names, boolean/property/enum/empty labels, card text, metadata, groups, zones, boards, backs, and dynamic messages using canonical control keys; verify active/new views, duplicate labels, empty-filter bits, and unselected game-list refresh without downloads.
- [ ] 4.6 Add interactive translated search separately from canonical `FilterCards`, deck imports, and automatic `DeckPlayCards` queries; verify translated/canonical name collisions affect only interactive results and canonical filters/selections remain stable.
- [ ] 4.7 Add versioned local authored-deck provenance sidecars tied to installed artifacts; verify first authored install, legacy/unproven files, identical-name/content user replacements, rename/save-as, externally changed fingerprints, and package versus standalone-deck import.
- [ ] 4.8 Add named-message capability negotiation and idempotent versioned stack creation with canonical label references, preserving existing RPCs and creation rules; verify duplicate-name creations, request retries, capable/legacy host-client combinations, timeout/unsupported-version fallback, and no extension for unlocalized games.
- [ ] 4.9 Implement authoritative provenance snapshots and revision/incarnation handling; verify late join, reconnect, rename/despawn races, invalid targets, and matching/mismatched session-save sidecars without transmitting localized text.
- [ ] 4.10 Document provenance lifecycle, canonical fallback without provenance, cached versus external rulebooks, and legacy-editor limitations; verify documented installation, export/import, and rollback against example games.
- [ ] 4.11 Add locale-reachability and coverage diagnostics using the actual selectable locale set plus default chains; verify `es-ES`/`zh-CN` warnings when unreachable without rejecting valid tags or enabling sibling matching.

## 5. Verify the complete integration

- [ ] 5.1 Exercise original English/Spanish/Japanese games, complete/partial catalogs, startup selection, repeated language switches, long labels, multiline rules, supported fonts, and offline use; record results against the surface inventory on desktop and mobile layouts.
- [ ] 5.2 Verify different-language and mixed-capability participants can load/play/save with identical canonical gameplay state/messages; confirm verified names translate only where provenance is available and locale changes emit no gameplay/provenance updates.
- [ ] 5.3 Verify feature rollback by removing localization on an in-place reload, offline package relocation, and unselected-game refresh; confirm unchanged old games need no edits and retain existing files/exports/assets, and record the CGS release dependency.

## Workflow follow-up

- Keep generated OpenSpec 1.16.0 skills unmodified. Record upstream template limitations noted in design section 9; this proposal uses supported dash task markers and `spec-driven` artifact IDs. Verify both declared capability files exist independently of the CLI's glob-completion status.
