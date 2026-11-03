# Tasks

Implementation progress is tracked below. Sections 1–3 belong to this repository; sections 4–5 require the separate Card-Game-Simulator repository. Keep downstream work visible until the user-facing feature is verified.

## 1. Add the format and schemas

- [x] 1.1 Add nullable localization metadata and typed catalog/source models, including `cardNames` and boolean labels, preserving canonical constructors/getters and exact dictionary keys; verify legacy deserialize/serialize fixtures and mixed-case ID round trips.
- [x] 1.2 Extend schema generation with `GameLocalization.json` and the optional manifest shape, including URI references and exclusive URL/inline sources; regenerate schemas and verify positive/negative fixtures for old and new definitions.
- [x] 1.3 Implement semantic diagnostics for tags, normalized duplicates, invalid leaves, ineligible property overrides, duplicate control labels, conflicting default-set targets, and unknown targets; verify observed literal enum tokens are accepted without registering lookups and invalid entries cannot block base data.
- [x] 1.4 Document every catalog member, reference origin, target key convention, empty/sentinel behavior, and older-schema/editor compatibility boundary in author guidance linked from README; validate documented JSON with active URI-format checking and warn that old editors can discard new metadata.

## 2. Add locale-independent presentation resolution

- [x] 2.1 Implement explicit-locale, per-leaf resolution; verify exact/parent/default/base fallback, case matching, invalid UI locales, extension truncation, deliberate empty strings, and no sibling/script guessing.
- [x] 2.2 Implement display resolution for names, nested/list text, boolean/empty labels, defined/literal enum tokens, zones, groups, boards, and backs; verify parsed values/flags stay unchanged and `game.setNameDefault` is authoritative for the default set.
- [x] 2.3 Implement localized resource candidate resolution using explicit recorded origins/package locations; verify inline/external/local cases, update-URL independence, missing-origin fallback, package path confinement, and unchanged legacy image templates.
- [x] 2.4 Add canonical target enumeration and coverage reporting; verify generated face/reprint IDs, canonical-name targets, custom identifiers, flattened/indexed paths, and orphan translations are exposed without guessing source layout.
- [x] 2.5 Document the display API and integration boundaries; verify examples resolve using explicit locale contexts without Unity dependencies or mutation of existing objects.
- [x] 2.6 Implement exact-ID then canonical-name fallback within each locale candidate; verify reprint load-order changes, face-specific names, shared canonical names, exact overrides, and name-mirroring properties without suffix stripping.

## 3. Verify library backwards compatibility

- [x] 3.1 Add English-, Spanish-, and Japanese-base fixtures with and without localization; verify legacy parsing, original display output, no localization requests, and omission of absent metadata.
- [x] 3.2 Cover every supported deck export format, reprint grouping, enum flags/literal tokens, booleans, extras, custom identifiers, back faces, and URL substitutions; verify outputs, parsing, and canonical queries remain identical across locales.
- [x] 3.3 Convert proposal examples into maintained author fixtures when the schema is implemented; verify original definitions against old/new schemas and localized definitions against the new schema using an active URI-format checker, including a negative relative-legacy-URL case.

## 4. Integrate in CGS (downstream repository)

- [x] 4.1 Create the corresponding CGS change and adopt the updated package; deliver a display-surface inventory and verify canonical data flows remain separate from UI bindings.
- [x] 4.2 Connect the locale event to catalog loading and generation-scoped refresh; reset localization only in a staged definition before repopulating it, then atomically replace live state and advance its generation only after loading and validation succeed and the definition is installable. Verify failed reads/validation retain the previous state and generation, alongside inline replacement at the same URL, absent/null removal, `autoUpdate < 0`, and rapid switches without restart.
- [ ] 4.3 Persist definition/catalog origins and portable cache mappings with per-key ordering by request initiation and atomic publication; verify that when A starts before B, B publishes successfully, and A completes afterward, B's entry survives across locale switches/same-URI reloads and offline reopen/export. Also verify failed refreshes preserve last-good data without advancing the successful-publication order, cache replacement rejects prior-store writes, and origin resolution, path confinement, and relocation remain correct. Include the companion cgs-games final-URL header, redirect validation, CORS exposure, cached-response retention, and missing/invalid-header fallback in native/WebGL transport verification.
- [x] 4.4 Add localized image loading with separately ordered atomic caches and rulebook link selection; verify the same initiation-order race as 4.3 keeps the later-initiated successful image when the earlier request completes afterward, failed regional variants fall back without overwriting originals, and cached images survive offline reopen/export/import.
- [ ] 4.5 Bind names, boolean/property/enum/empty labels, card text, metadata, groups, zones, boards, backs, and dynamic messages using canonical control keys; verify active/new views, duplicate labels, empty-filter bits, and unselected game-list refresh without downloads.
- [x] 4.6 Add interactive translated search separately from canonical `FilterCards`, deck imports, and automatic `DeckPlayCards` queries; verify translated/canonical name collisions affect only interactive results and canonical filters/selections remain stable.
- [x] 4.7 Add versioned local authored-deck provenance sidecars tied to installed artifacts; verify first authored install, legacy/unproven files, identical-name/content user replacements, rename/save-as, externally changed fingerprints, and package versus standalone-deck import.
- [ ] 4.8 Add named-message capability negotiation and idempotent versioned stack creation with canonical label references, preserving existing RPCs and creation rules; verify duplicate-name creations, request retries, capable/legacy host-client combinations, timeout/unsupported-version fallback, and no extension for unlocalized games.
- [x] 4.9 Implement authoritative provenance snapshots and revision/incarnation handling; verify late join, reconnect, rename/despawn races, and invalid targets without transmitting localized text. Session-sidecar implementation and matching/mismatched saved-document tests are conditional on a future session save/load feature and are not a current release gate.
- [ ] 4.10 Document provenance lifecycle, canonical fallback without provenance, cached versus external rulebooks, and legacy-editor limitations; verify documented installation, export/import, and rollback against example games.
- [x] 4.11 Add locale-reachability and coverage diagnostics using the actual selectable locale set plus default chains; verify `es-ES`/`zh-CN` warnings when unreachable without rejecting valid tags or enabling sibling matching.

## 5. Verify the complete integration

- [ ] 5.1 Exercise original English/Spanish/Japanese games, complete/partial catalogs, startup selection, repeated language switches, long labels, multiline rules, supported fonts, and offline use; record results against the surface inventory on desktop and mobile layouts.
- [ ] 5.2 Verify different-language and mixed-capability participants can load decks, play, and save decks with identical canonical gameplay state/messages; confirm verified names translate only where provenance is available and locale changes emit no gameplay/provenance updates.
- [ ] 5.3 Verify feature rollback by removing localization on an in-place reload, offline package relocation, and unselected-game refresh; confirm unchanged old games need no edits and retain existing files/exports/assets, and record the CGS release dependency.

## Workflow follow-up

- Keep generated OpenSpec 1.16.0 skills unmodified. Record upstream template limitations noted in design section 9; this proposal uses supported dash task markers and `spec-driven` artifact IDs. Verify both declared capability files exist independently of the CLI's glob-completion status.
