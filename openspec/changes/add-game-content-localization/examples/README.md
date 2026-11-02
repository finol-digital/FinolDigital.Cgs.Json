# Proposed authoring example

These synthetic files illustrate the proposed format; they are not an implemented feature or a complete downloadable game. URLs under `example.invalid` are placeholders, and no artwork/deck files are supplied. The current published `cgs.json` schema does not yet accept `localization`.

- [cgs.json](cgs.json) retains English canonical data, links an external Spanish catalog, and embeds a partial regional and Japanese catalog.
- [AllCards.json](AllCards.json) retains ordinary string fields and canonical IDs.
- [locales/es.json](locales/es.json) adds display strings and optional localized assets without rewriting canonical data.

Expected behavior:

- Spanish shows `Llama`, `Mazo inicial`, `Reglas`, and `Fuego`; card `002` displays `Sin reglas` for its empty rules property.
- `es-MX` shows `Flama`, inherits Spanish rules/property labels/enum labels, and falls back to English wherever both catalogs omit an entry.
- Japanese shows `炎` and `スターターデッキ`; untranslated card `002` retains `Wave`.
- An unsupported language uses the English base text because this example declares `en`. A game authored in Spanish or Japanese would declare `es` or `ja`, retain that original source text, and need no English catalog.
- Card ID `001`, canonical name `Flame`, enum key `fire`, deck key/file name `Starter`, and legacy image URL substitutions stay unchanged in every language.
- The Spanish image path resolves relative to `locales/es.json`: `locales/images/001.png`. Its absence falls back to the existing image.

For an existing game, opting in requires adding the optional manifest and authored catalogs only. Removing that manifest restores the original presentation without migrating card/deck/set data.
