# Localization authoring example

These maintained fixtures exercise the library format, schema, and resolver. They are not a complete downloadable game: URLs under `example.invalid` are placeholders, and no artwork/deck files are supplied. CGS UI behavior described below requires the separately tracked consumer integration.

- [cgs.json](cgs.json) retains English canonical data, links an external Spanish catalog, and embeds a partial regional and Japanese catalog.
- [AllCards.json](AllCards.json) is bundled in the game directory and retains ordinary string fields and canonical IDs. The definition deliberately omits `allCardsUrl`; existing URL fields still require absolute URIs. Only the new localization references support relative paths.
- [locales/es.json](locales/es.json) adds display strings and optional localized assets without rewriting canonical data.

Expected behavior:

- Spanish shows `Llama` for both printings of `Flame`, `Reglas`, and `Fuego`; card `002` displays `Sin reglas` for its empty rules property. Boolean `yes` and `1` display `Sí`, and `no` displays `No`, without changing their source values.
- A verified authored installation of `Starter` displays `Mazo inicial`; an unproven legacy or user-created deck with that name remains `Starter`. Shared stacks require negotiated provenance support; legacy participants retain canonical names.
- At the resolver level, `es-MX` shows `Flama` for card `001`, inherits `Llama` for the other printing and Spanish property/enum labels, and falls back to English where both catalogs omit an entry. CGS currently offers `es`, not `es-MX`: this regional entry demonstrates the format and should produce an unreachable-locale warning with today's UI/default configuration.
- Japanese shows `炎` and `スターターデッキ`; untranslated card `002` retains `Wave`.
- An unsupported language uses the English base text because this example declares `en`. A game authored in Spanish or Japanese would declare `es` or `ja`, retain that original source text, and need no English catalog.
- Card ID `001`, canonical name `Flame`, enum key `fire`, deck key/file name `Starter`, and legacy image URL substitutions stay unchanged in every language.
- The Spanish image path resolves relative to `locales/es.json`: `locales/images/001.png`. Its absence falls back to the existing image.
- Remote deployments must record the actual definition/catalog download origins separately from update URLs. Cached catalogs/artwork and their mappings live inside the game directory so package export/import and offline use retain their meaning.

For an existing game, opting in requires adding the optional manifest and authored catalogs; localization-aware CGS records origins and authored-deck provenance as consumer metadata. No existing card/deck/set file format changes. Removing localization on a successful in-place manifest reload restores original presentation immediately. Keep the source manifest when using older CGS editors: they can read base content but discard unknown localization metadata on save.
